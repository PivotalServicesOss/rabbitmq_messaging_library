namespace Unit.Test
{
    public class GlobalContainerTests
    {
        public GlobalContainerTests()
        {
            Global.ConsumerTypes.Add(typeof(string));
            Global.ConsumerTypes.Add(typeof(int));
        }

        [Fact]
        public void CheckProperties()
        {
            Global.ConsumerTypes.Should().HaveCount(2);

            Global.ConsumerTypes.Should().Contain(typeof(string));
            Global.ConsumerTypes.Should().Contain(typeof(int));
        }
    }
}
