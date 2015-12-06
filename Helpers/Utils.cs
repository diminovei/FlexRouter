using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace FlexRouter.Helpers
{
    class Utils
    {
        public static bool AreStringsEqual(string one, string two)
        {
            if (one == string.Empty)
                one = null;
            if (two == string.Empty)
                two = null;
            return one == two;
        }
        public static int GetNewId(IDictionary collection)
        {
            var i = 0;
            while (true)
            {
                if (!collection.Contains(i))
                    return i;
                i++;
            }
        }
        public static bool IsNumeric(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
/*            foreach (var c in text)
                if ((c < '0' || c > '9') && c != '-' && c != '.' && c != ',')
                    return false;
            return true;*/
            var regex = new Regex(@"^-?\d*\.?\d*$"); //regex that matches disallowed text
            //            var regex = new Regex(@"^-?\[0-9.]"); //regex that matches disallowed text
            return regex.IsMatch(text);
        }

        public static bool IsHexNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            int offset;
            return int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
        }

        static public string GetFullSubfolderPath(string subFolder)
        {
            var location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var folder = Path.Combine(location, subFolder);
            return folder;
        }

        //public class ProfileInfo
        //{
        //    public ProfileInfo(string name, string path)
        //    {
        //        Name = name;
        //        Path = path;
        //    }
        //    public string Name { get; set; }
        //    public string Path { get; set; }
        //}
        ///// <summary>
        ///// Получить список описаний профилей
        ///// </summary>
        ///// <param name="subFolder">Подкаталог в каталоге приложения, где располагаются профили</param>
        ///// <param name="fileMask">Маска файлов, среди которых нужно искать профили</param>
        ///// <param name="mainXmlNodeName">Начальный узел XML</param>
        ///// <param name="profileType">Тип профиля (профиль, назначения, ...)</param>
        ///// <returns></returns>
        //static public List<ProfileInfo> GetXmlList(string subFolder, string fileMask, string mainXmlNodeName, string profileType)
        //{
        //    var profileList = new List<ProfileInfo>();
        //    var folder = GetFullSubfolderPath(subFolder);

        //    if (!Directory.Exists(folder))
        //        Directory.CreateDirectory(folder);

        //    var fileList = Directory.GetFiles(folder, fileMask);

        //    foreach (var file in fileList)
        //    {
        //        var xp = new XPathDocument(file);
        //        var nav = xp.CreateNavigator();

        //        var mainKeyNav = nav.Select("/" + mainXmlNodeName);
        //        if (!mainKeyNav.MoveNext())
        //            continue;
        //        var fileProfileType = mainKeyNav.Current.GetAttribute("Type", mainKeyNav.Current.NamespaceURI);
        //        if (profileType != fileProfileType)
        //            continue;
        //        var value = mainKeyNav.Current.GetAttribute("Name", mainKeyNav.Current.NamespaceURI);
        //        profileList.Add(new ProfileInfo(value, file));
        //    }
        //    return profileList;
        //}


        static public Dictionary<string, string> GetXmlList(string subFolder, string fileMask, string mainXmlNodeName, string profileType)
        {
            var profileList = new Dictionary<string, string>();
            var folder = GetFullSubfolderPath(subFolder);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileList = Directory.GetFiles(folder, fileMask);

            foreach (var file in fileList)
            {
                try
                {
                    var xp = new XPathDocument(file);
                    var nav = xp.CreateNavigator();

                    var mainKeyNav = nav.Select("/" + mainXmlNodeName);
                    if (!mainKeyNav.MoveNext())
                        continue;
                    var fileProfileType = mainKeyNav.Current.GetAttribute("Type", mainKeyNav.Current.NamespaceURI);
                    if (profileType != fileProfileType)
                        continue;
                    var value = mainKeyNav.Current.GetAttribute("Name", mainKeyNav.Current.NamespaceURI);
                    profileList.Add(value, file);
                }
                catch (Exception ex)
                {
                }
            }
            return profileList;
        }
/*        public static T FindAndCreate<T>(bool localOnly, bool exportedOnly)
        {
            var types = FindAssignableClasses(typeof(T), localOnly, exportedOnly, false);
            if (types.Length != 0)
                return default(T);
            //if (types.Length == 0)
            //{
            //    return default(T);
            //}
            //if (types.Length != 1)
            //{
            //    Log.Warn(typeof(ReflectionUtil),
            //             "FindAndCreate found {0} instances of {1} whereas only 1 was expected.  Using {2}.  {3}",
            //             types.Length,
            //             typeof(T).FullName,
            //             types[0].FullName,
            //             String.Join("\r\n  ", Array.ConvertAll<Type, String>(types, GetFullName)));
            //}
            try
            {
                return (T)Activator.CreateInstance(types[0]);
            }
            catch (Exception ex)
            {
                //throw ExceptionUtil.Rethrow(ex,
                //                            "Unable to create instance of {0} found for interface {1}.",
                //                            types[0].FullName,
                //                            typeof(T).FullName);
            }
        }

        public static Type[] FindAssignableClasses(Type assignable, bool localOnly, bool exportedOnly, bool loadDll)
        {
            var list = new List<Type>();
            //string localDirectoryName = Path.GetDirectoryName(typeof(FlexRouter).Assembly.CodeBase);

            //if (loadDll && !_loadedAllDlls)
            //{
            //    foreach (string dllPath in Directory.GetFiles(localDirectoryName.Substring(6), "*.dll"))
            //    {
            //        try
            //        {
            //            Assembly.LoadFrom(dllPath);
            //        }
            //        catch
            //        {
            //            // ignore
            //        }
            //    }
            //    _loadedAllDlls = true;
            //}

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    //if (localOnly && Path.GetDirectoryName(asm.CodeBase) != localDirectoryName)
                    //{
                    //    continue;
                    //}

                    Type[] typesInAssembly;
                    try
                    {
                        typesInAssembly = exportedOnly ? asm.GetExportedTypes() : asm.GetTypes();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (Type t in typesInAssembly)
                    {
                        try
                        {
                            if (assignable.IsAssignableFrom(t) && assignable != t)
                            {
                                list.Add(t);
                            }
                        }
                        catch (Exception ex)
                        {
                            //Log.Debug(
                            //    typeof(ReflectionUtil),
                            //    String.Format(
                            //        "Error searching for types assignable to type {0} searching assembly {1} testing {2}{3}",
                            //        assignable.FullName,
                            //        asm.FullName,
                            //        t.FullName,
                            //        FlattenReflectionTypeLoadException(ex)),
                            //    ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //// ignore dynamic module error, no way to check for this condition first
                    //// http://groups.google.com/group/microsoft.public.dotnet.languages.csharp/browse_thread/thread/7b02223aefc6afba/c8f5bd05cc8b24b0
                    //if (!(ex is NotSupportedException && ex.Message.Contains("not supported in a dynamic")))
                    //{
                    //    Log.Debug(
                    //        typeof(ReflectionUtil),
                    //        String.Format(
                    //            "Error searching for types assignable to type {0} searching assembly {1} from {2}{3}",
                    //            assignable.FullName,
                    //            asm.FullName,
                    //            asm.CodeBase,
                    //            FlattenReflectionTypeLoadException(ex)),
                    //        ex);
                    //}
                }
            }

            return list.ToArray();
        }
        public static T FindAndCreateLocalClass<T>()
        {
            if (types.Length != 0)
                return default(T);
            //if (types.Length == 0)
            //{
            //    return default(T);
            //}
            //if (types.Length != 1)
            //{
            //    Log.Warn(typeof(ReflectionUtil),
            //             "FindAndCreate found {0} instances of {1} whereas only 1 was expected.  Using {2}.  {3}",
            //             types.Length,
            //             typeof(T).FullName,
            //             types[0].FullName,
            //             String.Join("\r\n  ", Array.ConvertAll<Type, String>(types, GetFullName)));
            //}
            try
            {
                return (T)Activator.CreateInstance(types[0]);
            }
            catch (Exception ex)
            {
                //throw ExceptionUtil.Rethrow(ex,
                //                            "Unable to create instance of {0} found for interface {1}.",
                //                            types[0].FullName,
                //                            typeof(T).FullName);
            }
        }*/

        public static T FindAndCreate<T>(string name, Object[] args = null)
//        public static Type[] FindLocalClassAndCreate(Type assignable, Object[] args)
        {
            var list = new List<Type>();
            try
            {
                var typesInAssembly = Assembly.GetEntryAssembly().GetTypes();
                foreach (var t in typesInAssembly)
                {
                    if (t.Name == name)
                    //if (typeof(T).IsAssignableFrom(t) && typeof(T) != t)
                    {
                        list.Add(t);
                    }
                }
                if (list.Count != 1)
                    return default(T);
                return args == null ? (T)Activator.CreateInstance(list[0]) : (T)Activator.CreateInstance(list[0], args);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
