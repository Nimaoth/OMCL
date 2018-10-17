# OMCL

Latest Version: 0.0.5

[Available as NuGet package](https://www.nuget.org/packages/OMCLConfig)

OMCL is a simple configuration language.
It supports comments, maps, arrays, strings, integers, floats, booleans and a special value, 'none', indication that no value is present.

Each value can be tagged.

.omcl files have to be valid UTF-8 files.

The top level of an omcl file is an object, where the curly braces can be omitted.

### Comments
There are two types of comments.

Line comments begin with ```//```:
```javascript
// This is a comment
```

Block comments are between `/*` and `*/`, and they can also be nested:
```javascript
/*
This is a block comment
    /*
        Block comments can be nested
    */
*/
```

### Objects
Objects are denoted by a pair of braces '{}'
```javascript
// Objects begin with an open brace
{
    // properties
}
```

#### Properties
Objects contain properties. Each property has a name and a value, separated by `=`

If the value is an object or an array, the `=` can be omitted.
```javascript
{
    // object with `=`
    property1 = {}

    // object without `=`
    // This is equivalent to property1
    property2 {}

    property3 = []
    property4 []

    property5 = "this is a string"

    "this property name contains whitespace" = "some value"
}
```

Propery names can contain any character except `"` if they are surrounded by double quotes, and `'` if they are surrounded by single quotes.


If a property name only contains the characters `a-z`, `A-Z`, `0-9`, `_` or `-`, it does not need to be surrounded with quotes.

### Lists
Objects are a list of unnamed values. Values have to separated by commas or a line break (`\n` or `\r\n`)

```javascript
// a list of integers
[ 1, 2, 3 ]

// the same list, but separated by line breaks
// instead of commas
[
    1
    2
    3
]

// lists can contain values of different types
[
    1
    "a string"
    'another string'
    {
        // a object
    }

    // a nested list
    [ "this", "is", 'a', 'list' ]
]

// commas linebreaks can be used together
// trailing commas are allowed
[
    1, 2, 3
    4, 5, 6,
    9,
    10,
    11
    12,
]
```

### Strings
Strings a character sequences surrounded by either single quotes (`'`) or double quotes (`"`).
They can contain all valid unicode codepoints except for the quotation character, even linebreaks.

String literals can be concatenated.

```
// A simple string literal
"This is a string literal"
'This is a string literal'

// With line breaks
"This string
contains linebreaks.
This is awesome"

// if you want to represent a string which contains both single and double quotes, you can use string concatenation:
"single quotes: '', " 'double quotes ""'
//                   ^ Here the string concatenation happens

// the resulting string will look like this:
// single quotes: '', double quotes ""
```

### Booleans
A bool can be either `true` or `false`
```javascript
true
false
```

### Integers
Integers are 64 bit signed numbers.
They can contain underscores to make them more readable.
Underscores can not appear at the beginning or end of the number literal. Hexadecimal numbers begin with `0x`, binary numbers with `0b` and octal numbers with `0o`
```
123
5_000_000
0xff        // decimal: 255
0b1000_1000 // decimal: 136
0o25        // decimal: 21
```

### Floats
Floats are 64 bit double presicion floating point numbers.
The rules for underscores are similiar to integers.
```
1.0
5_000_000.356_125
9.0e19
123e-5_000
```

### None
`none` is a special literal representing no value

### Tags
Each value can be tagged by prefixing it with a tag.
Tags have the form `!tag_name`, where tag_name can contain `a-z`, `A-Z`, `0-9`, `_` or `-`, but can not start or end with `_` or `-`
```javascript
// tag an object with `a_tag`
!a_tag {
    // ...
}

// multiple tags
!a !b !c { /* ... */ }

!regex "![a-zA-Z0-9]([a-zA-Z0-9-_]*[a-zA-Z0-9])*"

!int-list [ 1, 2, 3, 4, 5 ]
```

## Examples
```javascript
// properties
id = "Enemy1"
active = true
parent = none

// use tags to specify the type of component
components [
    !health_component {
        health = 100
    }

    !move_component {
        // ...
    }

    // ...
]
```

```javascript
// configuration for an imaginary service

endpoint = "some.random.url/some/path"
port     = 9876
auto_reload_config = true
reload_config_interval = !time_span "01:00" // 1 minute

authentication {
    username = "..."
    password = "..."
}

components [
    !some_compontent_1 {
        // compontent specific config
    }
    
    !some_compontent_2 {
        // compontent specific config
    }
]
```

## C# API examples

Parsing a .omcl file into C#-Objects can be done via `OMCL.Serialization.Parser`.
Parsing into a custom class structure will be implemented in future versions (available since version 0.0.5).
Serializing an object to a string or a file can be done with the `OMCL.Serialization.Serializer` class.

```csharp
using System;
using System.Text;
using OMCL.Data;
using OMCL.Serialization;

class Program
{
    static void Main(string[] args)
    {
        // parse from file
        Parser parser = Parser.FromFile("example.omcl");
        OMCLObject obj = parser.ParseObject();

        // parse from string (since version 0.0.4)
        parser = Parser.FromString(@"
            prop1 = 1
            prop2 = true
            prop3 {
                test1 = []
                test2 = {}
                test3 = none
            }
        ");
        obj = parser.ParseObject();

        // serialize to string
        var stringBuilder = new StringBuilder();
        Serializer serializer = Serializer.ToStringBuilder(stringBuilder);
        serializer.Serialize(obj);

        Console.WriteLine(stringBuilder);

        // serialize to file
        serializer = Serializer.ToFile("filename.omcl");
        serializer.Serialize(obj);
    }
}
```

Serialization into a custom class structure:
```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using OMCL.Data;
using OMCL.Serialization;

class Program
{
    // data structure
    class Person {
        public string Name { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime DateOfBirth;
        public float Height { get; set; }
        public string[] NickNames;
        public List<Person> Children;
        public BigInteger HairCount;
        public List<Hobby> Hobbies;
    }

    enum Gender {
        m, w
    }

    class Hobby {
        public string Name { get; set; }
    }

    class Soccer : Hobby {
        public string Position { get; set; }
    }

    class Tennis : Hobby {
        public bool Good { get; set; }
    }

    // converters
    class DateTimeConverter : IStringConverter {
        public object ConvertString(List<string> tags, string str) {
            return DateTime.ParseExact(str, "yyyy/MM/dd", CultureInfo.InvariantCulture);
        }
    }

    class BigIntegerConverter : IStringConverter {
        public object ConvertString(List<string> tags, string str) {
            return BigInteger.Parse(str);
        }
    }

    class HobbyConverter : IObjectConverter {
        public bool CanConvert(List<string> tags, OMCLObject obj) {
            return tags.Count == 1;
        }

        public object CreateInstance(List<string> tags) {
            var hobby = tags[0];
            switch (hobby) {
            case "Soccer": return new Soccer();
            case "Tennis": return new Tennis();
            default: return new Hobby();
            }
        }
    }

    // main
    static void Main(string[] args)
    {
        var parser = Parser.FromString(@"
            Name = 'Jon'
            LastName = 'Doe'
            Gender = 'm'
            Height = 1.82
            DateOfBirth = '1983/01/16'
            NickNames [ 'The One', 'And Only' ]
            Children [
                {
                    Name = 'Max'
                    LastName = 'Doe'
                    Gender = 'm'
                    // ...
                }
                {
                    Name = 'Gina'
                    LastName = 'Doe'
                    Gender = 'w'
                    // ...
                }
            ]
            HairCount = '12345678987654321234567898765432123456789'
            Hobbies [
                !Soccer {
                    Name = 'Soccer'
                    Position = 'Defense'
                }
                !Tennis {
                    Name = 'Tennis'
                    Good = false
                }
            ]
        ");

        Deserializer deserializer = new Deserializer();
        deserializer.AddStringConverter<DateTime>(new DateTimeConverter());
        deserializer.AddStringConverter<BigInteger>(new BigIntegerConverter());
        deserializer.AddObjectConverter<Hobby>(new HobbyConverter());

        var person = deserializer.Deserialize<Person>(parser);
        // ...
    }
}
```
