using AutoBrowser.Helpers;

namespace AutoBrowser.Tests.Helpers;

public class UlidHelperTests
{
    [Fact]
    public void NewUlid_GeneratesValidLengthString()
    {
        var ulid = UlidHelper.NewUlid();
        Assert.NotNull(ulid);
        Assert.Equal(26, ulid.Length);
    }

    [Fact]
    public void NewUlid_GeneratesCrockfordBase32CharactersOnly()
    {
        const string crockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        var ulid = UlidHelper.NewUlid();

        foreach (var c in ulid)
        {
            Assert.Contains(c, crockfordBase32);
        }
    }

    [Fact]
    public void NewUlid_GeneratesUniqueValues()
    {
        var set = new HashSet<string>();
        for (int i = 0; i < 1000; i++)
        {
            var ulid = UlidHelper.NewUlid();
            Assert.True(set.Add(ulid), $"Duplicate ULID generated: {ulid}");
        }
    }
}
