using EasyBuilders.Tests;



var person = Person.Builder()
    .WithName("Vitor")
    .Age(18)
    .Parent(Person.Builder()
        .WithName("Jackson")
        .Build())
    .Build();