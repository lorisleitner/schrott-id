# SchrottID

![Nuget](https://img.shields.io/nuget/dt/SchrottID?logo=nuget&label=NuGet)

SchrottID is a library for generating short, non-consecutive and opaque IDs from unsigned integers.
These can be used to obfuscate integer primary keys from your database and prevent enumeration of rows and other
confidential information
(see [German tank problem](https://de.wikipedia.org/wiki/German_tank_problem)).

SchrottIDs can be used where UUIDs would be impractical and too long to remember.

```csharp
// Choose a character set for your SchrottIDs
string alphabet = Alphabets.Base64;

// Generate a permutation for your alphabet
// The permutation is your "key" for encoding and decoding
// You cannot decode IDs correctly without this key 
string permutation = SchrottId.GeneratePermutation();

// Choose a minimum length for your SchrottIDs
int minimumLength = 3;

// Make sure to store the three parameters above

// Create a encoder/decoder
SchrottId schrottId = new SchrottId(alphabet, permutation, minimumLength);

// A primary key from your database
ulong primaryKey = 420;

// Encode primaryKey to a SchrottID that you can safely expose
string externalId = schrottId.Encode(primaryKey);
// externalId = 9TN

ulong decodedKey = schrottId.Decode(externalId);
// decodedKey = 420
```

Implementations in other languages should very similar APIs.

---

### Creating a new implementation

Implementations in other languages are very welcome. Create a new folder for your implementation and create a pull
request. Use `test/control.txt` to verify your implementation. `C#` is the reference implementation.

---

> This library is inspired by [block-id](https://github.com/drifting-in-space/block-id) and removes the dependency on a
> deterministic random number generator so implementations in other languages are easier. The libraries are not
> cross-compatible.

> Disclaimer: I'm not a cryptographer and IDs generated by this library can possibly be reversed engineered. Use with
> caution. 