using System.Collections.Generic;
using System.Linq;

namespace FlexRouter.Helpers
{
    public enum ProblemHideOnFixOptions
    {
        NotHide,
        HideDescription,
        HideItemAndDescription
    }
    public class Problem
    {
        public string Name;
        public string Description;
        public bool IsFixed;
        public ProblemHideOnFixOptions HideWhenFixed;
    }

    /// <summary>
    /// Класс хранение актуальных проблем
    /// </summary>
    static class Problems
    {
        /// <summary>
        /// Список актуальных проблем
        /// </summary>
        private static readonly Dictionary<string, Problem> ProblemList = new Dictionary<string, Problem>();
        /// <summary>
        /// Изменился ли список актуальных проблем?
        /// </summary>
        public static bool ProblemListWasChanged { get; private set; }

        /// <summary>
        /// Добавить в список новую проблему или обновить существующую
        /// </summary>
        /// <param name="name">Имя проблемы (ключ)</param>
        /// <param name="description">Описание проблемы</param>
        /// <param name="hideOnFixOptions">Скрывать ли и как информацию о проблеме после исправления</param>
        /// <param name="isFixed">Проблема устранена</param>
        public static void AddOrUpdateProblem(string name, string description, ProblemHideOnFixOptions hideOnFixOptions, bool isFixed)
        {
            lock (ProblemList)
            {
                if (!ProblemList.ContainsKey(name))
                    ProblemList.Add(name, new Problem());
                else
                    if (ProblemList[name].IsFixed == isFixed)
                        return;
                ProblemList[name].Name = name;
                ProblemList[name].IsFixed = isFixed;
                ProblemList[name].Description = description;
                ProblemList[name].HideWhenFixed = hideOnFixOptions;
                ProblemListWasChanged = true;
            }
        }

/*        /// <summary>
        /// Добавить в список новую проблему
        /// </summary>
        /// <param name="name">Имя проблемы (ключ)</param>
        /// <param name="description">Описание проблемы</param>
        public static void Raise(string name, string description)
        {
            lock (ProblemList)
            {
                if (!ProblemList.ContainsKey(name))
                    ProblemList.Add(name, new Problem());
                else
                    if (!ProblemList[name].IsFixed)
                        return;
                ProblemList[name].Name = name;
                ProblemList[name].IsFixed = false;
                ProblemList[name].Description = description;
                ProblemList[name].HideWhenFixed = false;
                ProblemListWasChanged = true;
            }
        }

        /// <summary>
        /// Проблема исправлена
        /// </summary>
        /// <param name="name">Имя проблемы (ключ)</param>
        /// <param name="description">Описание</param>
        /// <param name="hide">Скрыть или оставить в списке проблем со статусом 'исправлена'</param>
        public static void Fix(string name, string description, bool hide)
        {
            lock (ProblemList)
            {
                if (!ProblemList.ContainsKey(name))
                    ProblemList.Add(name, new Problem());
                else
                    if (ProblemList[name].IsFixed)
                        return;
                ProblemList[name].Name = name;
                ProblemList[name].IsFixed = true;
                ProblemList[name].Description = description;
                ProblemList[name].HideWhenFixed = hide;
                ProblemListWasChanged = true;
            }
        }*/

        /// <summary>
        /// Получить список проблем
        /// </summary>
        /// <returns>Массив проблем</returns>
        public static Problem[] GetProblemList()
        {
            lock (ProblemList)
            {
                ProblemListWasChanged = false;
                var list = new List<Problem>();
                foreach (Problem value in ProblemList.Values)
                {
                    if (value.IsFixed)
                    {
                        if (value.HideWhenFixed == ProblemHideOnFixOptions.NotHide)
                            list.Add(value);
                        if (value.HideWhenFixed == ProblemHideOnFixOptions.HideDescription)
                        {
                            value.Description = string.Empty;
                            list.Add(value);
                        }
                    }
                    else
                        list.Add(value);
                }
                return list.ToArray();
            }
        }
    }
}
