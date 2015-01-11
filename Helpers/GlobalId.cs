using System.Collections.Generic;

namespace FlexRouter.Helpers
{
    static class GlobalId
    {
        /// <summary>
        /// Список используемых глобальных идентификаторов
        /// </summary>
        static private readonly List<int> UsedIdList = new List<int>();
        /// <summary>
        /// Зарегистрировать имеющийся Id в списке (при загрузке профиля). id начинаются с единицы
        /// </summary>
        /// <returns>false - такого id не существует, true - успешно зарегистрирован</returns>
        static public bool RegisterExisting(int id)
        {
            lock (UsedIdList)
            {
                if (UsedIdList.Contains(id))
                    return false;
                UsedIdList.Add(id);
                return true;
            }
        }
        /// <summary>
        /// Получить новый глобальный Id. id начинаются с единицы
        /// 0 - Id не назначен, так как при назначении нового id переменная сначала увеличивается на единицу, а потом её значение отдаётся в качестве id
        /// </summary>
        /// <returns>id. 0 - Id не назначен, так как при назначении нового id переменная сначала увеличивается на единицу, а потом её значение отдаётся в качестве id</returns>
        static public int GetNew()
        {
            lock (UsedIdList)
            {
                var id = UsedIdList.Count == 0 ? 1 : UsedIdList[UsedIdList.Count-1];
                while (true)
                {
                    if (!UsedIdList.Contains(id))
                    {
                        UsedIdList.Add(id);
                        return id;
                    }
                    id++;
                }
            }
        }
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
    }
}
