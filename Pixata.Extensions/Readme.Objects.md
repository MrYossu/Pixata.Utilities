# Object extension methods

>Part of [Pixata.Extensions](Readme.md)

Extension methods that apply to any object, found in `ObjectExtensionMethods`.

`Base64Encode()` - Encodes a byte array to a base64-encoded string, suitable for using as an embedded image in HTML. Assumes a jpg image, but this can be overridden by supplying a different `mimeType` parameter, eg "png".

`Clone()` - Returns a shallow clone of an object. Uses reflection, but despite all the myths about this being slow, doing 10 million clones only took about 300ms longer than a reflection-free (and way more complex) version, so I went for simplicity.

`DumpProperties()` - Dumps the names and values of all simple properties of an object to a string for debugging purposes. Simple properties are enums, `string`, `decimal`, `DateTime`, `DateOnly`, `TimeOnly`, `TimeSpan` and `Guid`. Properties that cannot be read, or that are in the optional `ignoredProperties` list, are skipped.
