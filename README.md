# OMCL
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

```javascript
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
```javascript
123
5_000_000
0xff        // decimal: 255
0b1000_1000 // decimal: 136
0o25        // decimal: 21
```

### Floats
Floats are 64 bit double presicion floating point numbers.
The rules for underscores are similiar to integers.
```javascript
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
// game object
// use a tag to specify that this object should be serialized into a game object (just a example)
!game_object

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