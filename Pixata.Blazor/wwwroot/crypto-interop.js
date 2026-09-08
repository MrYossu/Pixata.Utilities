let keyPair = null;
let aesKey = null;

export async function generateKeyPair() {
    keyPair = await crypto.subtle.generateKey(
        { name: "ECDH", namedCurve: "P-256" },
        false,
        ["deriveBits"]
    );
    const raw = await crypto.subtle.exportKey("raw", keyPair.publicKey);
    return new Uint8Array(raw);
}

export async function deriveSessionKey(serverPublicKeyBytes, siteId) {
    const serverKey = await crypto.subtle.importKey(
        "raw",
        new Uint8Array(serverPublicKeyBytes),
        { name: "ECDH", namedCurve: "P-256" },
        false,
        []
    );

    const sharedBits = await crypto.subtle.deriveBits(
        { name: "ECDH", public: serverKey },
        keyPair.privateKey,
        256
    );

    const hkdfKey = await crypto.subtle.importKey(
        "raw",
        sharedBits,
        "HKDF",
        false,
        ["deriveKey"]
    );

    const encoder = new TextEncoder();
    aesKey = await crypto.subtle.deriveKey(
        {
            name: "HKDF",
            hash: "SHA-256",
            salt: new Uint8Array(32),
            info: encoder.encode("EncodeData-" + siteId)
        },
        hkdfKey,
        { name: "AES-GCM", length: 256 },
        false,
        ["encrypt", "decrypt"]
    );
}

export async function encrypt(plaintext) {
    const iv = crypto.getRandomValues(new Uint8Array(12));
    const ciphertext = await crypto.subtle.encrypt(
        { name: "AES-GCM", iv },
        aesKey,
        new Uint8Array(plaintext)
    );
    const result = new Uint8Array(12 + ciphertext.byteLength);
    result.set(iv);
    result.set(new Uint8Array(ciphertext), 12);
    return result;
}

export async function decrypt(data) {
    const bytes = new Uint8Array(data);
    const iv = bytes.slice(0, 12);
    const ciphertext = bytes.slice(12);
    const plaintext = await crypto.subtle.decrypt(
        { name: "AES-GCM", iv },
        aesKey,
        ciphertext
    );
    return new Uint8Array(plaintext);
}
