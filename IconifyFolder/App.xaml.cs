using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace IconifyFolder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        /// <summary>
        /// 向外暴露，Gets the Current <see cref="App"/> instance in use
        /// </summary>
        public static App Current = (App)System.Windows.Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; set; }

        public App()
        {
            Services = ConfigureServices();
        }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 注入视图和视图模型
            RegisterViewsAndViewModels(services);

            return services.BuildServiceProvider();
        }

        #region 注册视图和视图模型

        /// <summary>
        /// 注册视图和视图模型
        /// </summary>
        /// <param name="services"></param>
        private static void RegisterViewsAndViewModels(ServiceCollection services)
        {
            try
            {
                // 获取所有视图和视图模型类型
                var viewNamespace = typeof(App).Namespace + ".Views";
                var viewModelNamespace = typeof(App).Namespace + ".ViewModels";

                var viewTypes = typeof(App).Assembly.GetTypes()
                    .Where(t => t.Namespace == viewNamespace && t.Name.EndsWith("View"));
                var viewModelTypes = typeof(App).Assembly.GetTypes()
                    .Where(t => t.Namespace == viewModelNamespace && t.Name.EndsWith("ViewModel"));

                // 注入视图和视图模型
                foreach (var viewType in viewTypes)
                {
                    services.AddSingleton(viewType);
                }

                foreach (var viewModelType in viewModelTypes)
                {
                    services.AddSingleton(viewModelType);
                }
            }
            catch (Exception ex)
            {
                // 记录异常日志
                Console.WriteLine($"Error registering views and view models: {ex.Message}");
                throw;
            }
        }

        #endregion 注册视图和视图模型
    }
}