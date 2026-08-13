using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Utils.Extensions
{
    /// <summary>
    /// Provides extension methods for types.
    /// </summary>
    public static class TypedExtensions
    {
        /// <summary>
        /// Returns the default value for the specified type.
        /// </summary>
        /// <param name="targetType">The type to get the default value for.</param>
        /// <returns>The default value for the specified type.</returns>
        public static object ToDefault(this Type targetType)
        {

            if (targetType == null)
                throw new NullReferenceException();

            var mi = typeof(TypedExtensions)
                .GetMethod("_ToDefaultHelper", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            var generic = mi.MakeGenericMethod(targetType);

            var returnValue = generic.Invoke(null, new object[0]);
            return returnValue;
        }

        static T _ToDefaultHelper<T>()
        {
            return default(T);
        }
       
        /// <summary>
        /// Determines whether the specified type has implemented the specified generic type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="generic">The generic type to check for.</param>
        /// <returns>true if the specified type has implemented the specified generic type; otherwise, false.</returns>
        public static bool HasImplementedRawGeneric(this Type type, Type generic)
        {
            // Check interfaces
            var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
            if (isTheRawGenericType) return true;

            // Check type
            while (type != null && type != typeof(object))
            {
                isTheRawGenericType = IsTheRawGenericType(type);
                if (isTheRawGenericType) return true;
                type = type.BaseType;
            }

            return false;

            // Local function to check if a type is the raw generic type
            bool IsTheRawGenericType(Type type) => generic == (type.IsGenericType ? type.GetGenericTypeDefinition() : type);
        }
    }
}

