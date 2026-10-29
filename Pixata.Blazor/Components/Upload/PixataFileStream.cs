using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Pixata.Blazor.Components;

/// <summary>
/// A read-only stream over a file the user picked, which is read from the browser (as an <see cref="IJSStreamReference"/>)
/// the first time it is read. That works the same in Blazor Server and WASM, but means the stream can only be read
/// asynchronously
/// </summary>
internal sealed class PixataFileStream(long length, long maxAllowedSize, Func<CancellationToken, ValueTask<IJSStreamReference>> getFile, CancellationToken openCancellationToken) : Stream {
  private IJSStreamReference? _reference;
  private Stream? _inner;
  private long _position;
  private bool _finished;
  private bool _disposed;

  public override bool CanRead => true;
  public override bool CanSeek => false;
  public override bool CanWrite => false;
  public override long Length => length;

  public override long Position {
    get => _position;
    set => throw new NotSupportedException();
  }

  public override void Flush() {
  }

  public override int Read(byte[] buffer, int offset, int count) =>
    throw new NotSupportedException("The file is read from the browser, so it has to be read asynchronously, eg with CopyToAsync() or ReadAsync()");

  public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
    ObjectDisposedException.ThrowIf(_disposed, this);
    if (_finished || buffer.Length == 0) {
      return 0;
    }
    Stream inner = await Inner(cancellationToken);
    int read = await inner.ReadAsync(buffer, cancellationToken);
    _position += read;
    if (read == 0) {
      // Let go of the browser's file as soon as it has all been read, as code that reads a stream often doesn't dispose it,
      // and the browser (and in Blazor Server, the server) would otherwise hold on to it for as long as the user is here
      _finished = true;
      await Release();
    }
    return read;
  }

  public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
    ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

  public override long Seek(long offset, SeekOrigin origin) =>
    throw new NotSupportedException();

  public override void SetLength(long value) =>
    throw new NotSupportedException();

  public override void Write(byte[] buffer, int offset, int count) =>
    throw new NotSupportedException();

  private async ValueTask<Stream> Inner(CancellationToken cancellationToken) {
    if (_inner is not null) {
      return _inner;
    }
    using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, openCancellationToken);
    try {
      _reference = await getFile(linked.Token);
      _inner = await _reference.OpenReadStreamAsync(maxAllowedSize, linked.Token);
    }
    catch (JSException ex) {
      throw new IOException("The browser no longer has the file. Read it before it is removed from the list", ex);
    }
    return _inner;
  }

  protected override void Dispose(bool disposing) {
    if (disposing) {
      _disposed = true;
      _inner?.Dispose();
      _inner = null;
      _ = DisposeReference();
    }
    base.Dispose(disposing);
  }

  public override async ValueTask DisposeAsync() {
    _disposed = true;
    await Release();
    await base.DisposeAsync();
  }

  private async Task Release() {
    if (_inner is not null) {
      await _inner.DisposeAsync();
      _inner = null;
    }
    await DisposeReference();
  }

  private async Task DisposeReference() {
    IJSStreamReference? reference = _reference;
    _reference = null;
    if (reference is null) {
      return;
    }
    try {
      await reference.DisposeAsync();
    }
    catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException) {
    }
    catch (InvalidOperationException) {
    }
  }
}
