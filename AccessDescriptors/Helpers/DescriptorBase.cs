using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.CalculatorRelated;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorBase : IAccessDescriptor
    {
        private DescriptorBase _parentAccessDescriptorId;
        public void SetDependency(DescriptorBase parentAccessDescriptorId)
        {
            _parentAccessDescriptorId = parentAccessDescriptorId;
        }
        public void ResetDependency()
        {
            _parentAccessDescriptorId = null;
        }
        public bool IsDependent()
        {
            return _parentAccessDescriptorId != null;
        }
        public DescriptorBase GetDependency()
        {
            return _parentAccessDescriptorId;
        }
        /// <summary>
        /// Глобальный идентификатор -1 - значит ещё не зарегистрирован в профиле
        /// </summary>
        protected int Id = -1;
        /// <summary>
        /// Имя AccessDescriptor'а
        /// </summary>
        private string _name;
        /// <summary>
        /// Идентификатор панели, к которой принадлежит AccessDescriptor
        /// </summary>
        private int _panelId = -1;
        /// <summary>
        /// Использовать формулу питания панели или собственную?
        /// </summary>
        private bool _usePanelPowerFormula;
        /// <summary>
        /// Идентификатор формулы питания в GlobalFormulaKeeper
        /// </summary>
        private int _powerFormulaId = -1;
        /// <summary>
        /// Аддон к калькулятору, позволяющий парсить переменные в формулах
        /// </summary>
        protected readonly CalculatorVariableAccessAddon CalculatorVariableAccessAddonE = new CalculatorVariableAccessAddon();
        /// <summary>
        /// Калькулятор
        /// </summary>
        protected readonly Calculator CalculatorE = new Calculator();

/*        public DescriptorBase Copy()
        {
            var descriptor = MemberwiseClone();
            ((DescriptorBase)descriptor).Id = GlobalId.GetNew();
            return (DescriptorBase)descriptor;
        }*/
        protected DescriptorBase()
        {
            Id = GlobalId.GetNew();
            CalculatorE.RegisterTokenizer(CalculatorVariableAccessAddonE.VariableTokenizer);
            CalculatorE.RegisterPreprocessor(CalculatorVariableAccessAddonE.VariablePreprocessor);
        }

        public bool IsPowerOn()
        {
            var result = CalculatorE.ComputeFormula(_usePanelPowerFormula ? Profile.GetPanelById(_panelId).GetPowerFormula() : GetPowerFormula());
            if (result.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.FormulaWasEmpty)
                return true;
            return result.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.BooleanResult && result.CalculatedBoolBoolValue;
        }

        public void SetPowerFormula(string powerFormula)
        {
            if (_powerFormulaId == -1)
                _powerFormulaId = GlobalFormulaKeeper.Instance.StoreFormula(powerFormula, GetId());
            else
                GlobalFormulaKeeper.Instance.ChangeFormulaText(_powerFormulaId, powerFormula);
//               GlobalFormulaKeeper.Instance.SetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.Power, GetId(), powerFormula);
        }

        public string GetPowerFormula()
        {
            return _powerFormulaId == -1 ? null : GlobalFormulaKeeper.Instance.GetFormulaText(_powerFormulaId);
            //return GlobalFormulaKeeper.Instance.GetFormula(FormulaKeeperItemType.AccessDescriptor, FormulaKeeperFormulaType.Power, GetId());        
        }

        public string GetName()
        {
            return _name;
        }

        public void SetName(string name)
        {
            _name = name;
        }

        /// <summary>
        ///  -1 - значит ещё не зарегистрирован в профиле
        /// </summary>
        public int GetId()
        {
            return Id;
        }

/*        public void SetId(int id)
        {
            _id = id;
        }*/

        public int GetAssignedPanelId()
        {
            return _panelId;
        }

        public void SetAssignedPanelId(int panelId)
        {
            _panelId = panelId;
        }

        public void SetUsePanelPowerFormulaFlag(bool on)
        {
            _usePanelPowerFormula = on;
        }

        public bool GetUsePanelPowerFormulaFlag()
        {
            return _usePanelPowerFormula;
        }

        public void Save(XmlWriter writer)
        {
            SaveHeader(writer);
            SaveAdditionals(writer);
        }

        public void SaveHeader(XmlWriter writer)
        {
            writer.WriteAttributeString("Type", GetType().Name);
            writer.WriteAttributeString("Id", Id.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("PanelId", _panelId.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Name", _name);
            writer.WriteAttributeString("PowerFormula", GetPowerFormula());
            writer.WriteAttributeString("UsePanelPowerFormula", _usePanelPowerFormula.ToString());
        }

        public virtual void SaveAdditionals(XmlWriter writer)
        {

        }

        public void Load(XPathNavigator reader)
        {
            LoadHeader(reader);
            LoadAdditionals(reader);
        }

        public void LoadHeader(XPathNavigator reader)
        {
            Id = int.Parse(reader.GetAttribute("Id", reader.NamespaceURI));
            GlobalId.RegisterExisting(Id);
            GlobalFormulaKeeper.Instance.RemoveFormulasByOwnerId(GetId());
            _panelId = int.Parse(reader.GetAttribute("PanelId", reader.NamespaceURI));
            _name = reader.GetAttribute("Name", reader.NamespaceURI);
            var powerFormula = reader.GetAttribute("PowerFormula", reader.NamespaceURI);
            SetPowerFormula(powerFormula);
            _usePanelPowerFormula = bool.Parse(reader.GetAttribute("UsePanelPowerFormula", reader.NamespaceURI));
        }

        public virtual void LoadAdditionals(XPathNavigator reader)
        {
        }

        public virtual void Initialize()
        {
        }
        public abstract string GetDescriptorType();
    }
}
