using EasyBuilders.Tests;

var person = Person.Builder()
    .Name("Vitor")
    .Age(18)
    .A("dsadsa")
    .Build();

Console.WriteLine(person.name);