using System;
using System.Collections.Generic;

namespace billiard_management_system.Services
{
    /// <summary>
    /// Service Provider / Service Locator siêu nhẹ cho ứng dụng .NET Framework WPF
    /// Quản lý đăng ký và cấp phát singleton / transient service đơn giản không phụ thuộc thư viện ngoài nặng nề.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, Func<object>> _transients = new Dictionary<Type, Func<object>>();

        public static void RegisterSingleton<TInterface, TImplementation>(TImplementation instance) 
            where TImplementation : class, TInterface
        {
            _singletons[typeof(TInterface)] = instance;
        }

        public static void RegisterSingleton<TInterface>(Func<TInterface> factory)
        {
            _transients[typeof(TInterface)] = () => factory();
        }

        public static void RegisterTransient<TInterface>(Func<TInterface> factory)
        {
            _transients[typeof(TInterface)] = () => factory();
        }

        public static TInterface Resolve<TInterface>()
        {
            var type = typeof(TInterface);

            if (_singletons.TryGetValue(type, out var instance))
            {
                return (TInterface)instance;
            }

            if (_transients.TryGetValue(type, out var factory))
            {
                return (TInterface)factory();
            }

            throw new InvalidOperationException($"Dịch vụ '{type.FullName}' chưa được đăng ký trong ServiceLocator.");
        }
    }
}
