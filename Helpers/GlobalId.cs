using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FlexRouter.Helpers
{

    public enum ObjType
    {
        Variable,
        Panel,
        AccessDescriptor
    }
    public class PrepToGuid
    {
        public ObjType Type;
        public int OldId;
        public Guid NewId;
    }
    static class GlobalId
    {
        static List<PrepToGuid> prepToGuids = new List<PrepToGuid>();
        /// <summary>
        /// Список используемых глобальных идентификаторов
        /// </summary>
        static private readonly List<int> UsedIdList = new List<int>();
        /// <summary>
        /// Зарегистрировать имеющийся Id в списке (при загрузке профиля). id начинаются с единицы
        /// </summary>
        /// <returns>false - такого id не существует, true - успешно зарегистрирован</returns>
        static public Guid Register(ObjType type, int id)
        {
            var x = new PrepToGuid {Type = type, OldId = id, NewId = GetNew()};
            prepToGuids.Add(x);
            return x.NewId;
        }

        static public Guid GetByOldId(ObjType type, int id)
        {
            var z = prepToGuids.FirstOrDefault(x => x.Type == type && x.OldId == id);
            if(z == null)
            return Guid.Empty;
            return z.NewId;
        }

        static public void Save()
        {
            using (var file = new System.IO.StreamWriter(@"d:\idtoguid.txt"))
            {
                foreach (var s in prepToGuids)
                {
                        file.WriteLine(s.Type + ";" + s.OldId + ";" + s.NewId);
                }
            }            
        }

        /// <summary>
        /// Получить новый глобальный Id. id начинаются с единицы
        /// 0 - Id не назначен, так как при назначении нового id переменная сначала увеличивается на единицу, а потом её значение отдаётся в качестве id
        /// </summary>
        /// <returns>id. 0 - Id не назначен, так как при назначении нового id переменная сначала увеличивается на единицу, а потом её значение отдаётся в качестве id</returns>
        static public Guid IntToGuid(int value)
        {
            var guidAsString = "00000000-0000-0000-0000-000000000000";
            guidAsString = guidAsString.Remove(guidAsString.Length - value.ToString(CultureInfo.InvariantCulture).Length);
            guidAsString += value.ToString(CultureInfo.InvariantCulture);
            var guid = new Guid(guidAsString);
            return guid;
        }
        static public Guid GetNew()
        {
            return System.Guid.NewGuid();
        }
        ///// <summary>
        ///// Получить новый глобальный Id. id начинаются с единицы
        ///// 0 - Id не назначен, так как при назначении нового id переменная сначала увеличивается на единицу, а потом её значение отдаётся в качестве id
        ///// </summary>
        ///// <returns>id. 0 - Id не назначен, так как при назначении нового id переменная сначала увеличивается на единицу, а потом её значение отдаётся в качестве id</returns>
        //static public int GetNew()
        //{
        //    lock (UsedIdList)
        //    {
        //        var id = UsedIdList.Count == 0 ? 1 : UsedIdList[UsedIdList.Count-1];
        //        while (true)
        //        {
        //            if (!UsedIdList.Contains(id))
        //            {
        //                UsedIdList.Add(id);
        //                return id;
        //            }
        //            id++;
        //        }
        //    }
        //}
        /// <summary>
        /// Удалить имеющийся Id в списке (при загрузке профиля). id начинаются с единицы
        /// </summary>
        /// <returns>false - такого id не существует, true - успешно удалено</returns>
        static public bool Remove(int id)
        {
            lock (UsedIdList)
            {
                if (!UsedIdList.Contains(id))
                    return false;
                UsedIdList.Remove(id);
                return true;
            }
        }

        static public void ClearAll()
        {
            UsedIdList.Clear();
        }
    }
}
