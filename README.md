# Cheese
A strictly typed .NET xml serialization library used in my not yet released game engine.

It should've been a .NET Standard library but I'm to lazy to make it one.

Also it's not a NuGet package because I don't care lol.

## Usage

Members have to meet these conditions to be serialized or deserialized

1. Containing type defines `CheeseSerializable` attribute:
    - Public and non public members have to define `CheeseSerializable` attribute
2. Containing type doesn't define `CheeseSerializable` attribute
    - Only non public members have to define `CheeseSerializable` attribute

Fields cannot be readonly and properties have to define a getter and a setter.

### Example

```csharp
//Serialization
Cheese.Serialize(new Data(), "data.xml");

//Deserialization
Data data = Cheese.Deserialize<Data>("data.xml");
```
