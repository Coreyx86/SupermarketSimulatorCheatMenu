using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SupermarketSimulatorCheatMenu
{
    internal class Helper
    {
        public static bool TryGetField<X, T>(object a_parent, string a_fieldName, out T o_instance, BindingFlags a_bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            o_instance = default(T);

            try
            {
                Type genericType = typeof(X);
                FieldInfo genericTypeInfo = genericType.GetField(a_fieldName, a_bindingFlags);
                o_instance = (T)genericTypeInfo.GetValue(a_parent);
                return true;
            }
            catch (Exception e)
            {
            }

            return false;
        }

        public static bool TrySetField<X, T>(object a_parent, string a_fieldName, T value, BindingFlags a_bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance)
        {
            try
            {
                Type genericType = typeof(X);
                FieldInfo genericTypeInfo = genericType.GetField(a_fieldName, a_bindingFlags);
                genericTypeInfo.SetValue(a_parent, value);
                return true;
            }
            catch (Exception e)
            {
            }

            return false;
        }

        public static bool TryCallMethod<X,T>(object a_parent, string a_fieldName, Type[] paramTypes, out T o_retValues, params object[] a_params)
        {
            o_retValues = default(T);

            try
            {
                Type genericType = typeof(X);
                MethodInfo privateMethod = null;
                if (paramTypes == null)
                {
                    privateMethod = genericType.GetMethod(a_fieldName);
                }
                else
                {
                    privateMethod = genericType.GetMethod(a_fieldName, paramTypes);
                }

                o_retValues = (T)privateMethod.Invoke(a_parent, a_params);
                return true;
            }
            catch (Exception e)
            {

            }

            return false;
        }
    }
}
