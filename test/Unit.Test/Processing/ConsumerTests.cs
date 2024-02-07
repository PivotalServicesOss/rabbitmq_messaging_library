// namespace Unit.Test
// {
//     public class ConsumerTests
//     {
//         Mock<IOptions<ServiceConfiguration>> serviceConfigurationOptions;
//         Mock<IOptionsMonitor<QueueConfiguration>> queueConfigurationOptions;
//         Mock<ILogger<Consumer<string>>> logger;
//         Consumer<string> consumer;

//         public ConsumerTests()
//         {
//             serviceConfigurationOptions = new Mock<IOptions<ServiceConfiguration>>();
//             queueConfigurationOptions = new Mock<IOptionsMonitor<QueueConfiguration>>();
//             logger = new Mock<ILogger<Consumer<string>>>();
//             consumer = new Consumer<string>(serviceConfigurationOptions.Object,
//                                             queueConfigurationOptions.Object,
//                                             logger.Object);
//         }

//         [Fact]
//         public void StartConsumption_InitializesConnection()
//         {
            
//         }

//         [Fact]
//         public void ConsumerIs()
//         {
//             consumer.Should().BeAssignableTo<IDisposable>();
//             consumer.Should().BeAssignableTo<IConsumer>();
//             consumer.Should().BeAssignableTo<IConsumer<string>>();
//             consumer.Should().BeAssignableTo<Factory<string>>();
//         }
//     }
// }
