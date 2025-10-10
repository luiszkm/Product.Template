

using Bogus;

namespace CommonTests.Common;

public class BaseFixture
{
    public Faker Faker { get; } = new Faker();  


    public string RandomString(int minLength = 5, int maxLength = 20)
        => Faker.Random.String2(minLength, maxLength);

    public int RandomInt(int min = 1, int max = 1000)
        => Faker.Random.Int(min, max);

    public DateTime RandomDateTime(DateTime? min = null, DateTime? max = null)
        => Faker.Date.Between(min ?? DateTime.UtcNow.AddYears(-1), max ?? DateTime.UtcNow);

    public bool RandomBool()
        => Faker.Random.Bool();

    public T RandomEnumValue<T>() where T : Enum
        => Faker.PickRandom<T>();

    public List<T> RandomList<T>(Func<T> itemGenerator, int minItems = 1, int maxItems = 10)
        => Enumerable.Range(0, Faker.Random.Int(minItems, maxItems))
            .Select(_ => itemGenerator())
            .ToList();

    public string RandomEmail() => Faker.Internet.Email();
    public string RandomPhoneNumber() => Faker.Phone.PhoneNumber();
    public string RandomGuid() => Guid.NewGuid().ToString();



}
