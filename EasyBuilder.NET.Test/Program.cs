using EasyBuilders.Tests;

var person = Person.Builder()
    .Name("Vitor")
    .Age(18)
    .Parent(Person.Builder()
        .Name("Jackson")
        .Build())
    .Build();

Console.WriteLine(person.name);