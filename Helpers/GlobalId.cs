using System;
using System.Collections.Generic;
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
        static readonly List<PrepToGuid> PrepToGuids = new List<PrepToGuid>();
        /// <summary>
        /// Зарегистрировать имеющийся Id в списке (при загрузке профиля). id начинаются с единицы
        /// </summary>
        /// <returns>false - такого id не существует, true - успешно зарегистрирован</returns>
        static public Guid Register(ObjType type, int id)
        {
            var x = new PrepToGuid {Type = type, OldId = id, NewId = GetNew()};
            PrepToGuids.Add(x);
            return x.NewId;
        }

        static public Guid GetByOldId(ObjType type, int id)
        {
            var z = PrepToGuids.FirstOrDefault(x => x.Type == type && x.OldId == id);
            if(z == null)
            return Guid.Empty;
            return z.NewId;
        }

        //static public void Save()
        //{
        //    using (var file = new System.IO.StreamWriter(@"d:\idtoguid.txt"))
        //    {
        //        foreach (var s in prepToGuids)
        //        {
        //                file.WriteLine(s.Type + ";" + s.OldId + ";" + s.NewId);
        //        }
        //    }            
        //}

        static public Guid GetNew()
        {
            return Guid.NewGuid();
        }
    }
}
