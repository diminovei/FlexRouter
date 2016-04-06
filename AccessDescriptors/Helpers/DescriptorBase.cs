using System;
using System.Xml;
using System.Xml.XPath;
using FlexRouter.AccessDescriptors.FormulaKeeper;
using FlexRouter.AccessDescriptors.Interfaces;
using FlexRouter.CalculatorRelated;
using FlexRouter.Helpers;
using FlexRouter.ProfileItems;

namespace FlexRouter.AccessDescriptors.Helpers
{
    public abstract class DescriptorBase : ProfileItemPrivacy, IAccessDescriptor, ITreeItem
    {
        private DescriptorBase _parentAccessDescriptorId;

        public abstract Connector[] GetConnectors(object controlProcessor, bool withDefaultState = false);

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
        protected Guid Id = Guid.Empty;
        /// <summary>
        /// Имя AccessDescriptor'а
        /// </summary>
        private string _name;
        /// <summary>
        /// Идентификатор панели, к которой принадлежит AccessDescriptor
        /// </summary>
        private Guid _panelId = Guid.Empty;
        /// <summary>
        /// Использовать формулу питания панели или собственную?
        /// </summary>
        private bool _usePanelPowerFormula;
        /// <summary>
        /// Идентификатор формулы питания в GlobalFormulaKeeper
        /// </summary>
        private Guid _powerFormulaId = Guid.Empty;
        /// <summary>
        /// Аддон к калькулятору, позволяющий парсить переменные в формулах
        /// </summary>
        protected readonly CalculatorVariableAccessAddon CalculatorVariableAccessAddonE = new CalculatorVariableAccessAddon();
        /// <summary>
        /// Калькулятор
        /// </summary>
        protected readonly Calculator CalculatorE = new Calculator();

        protected DescriptorBase()
        {
            Id = GlobalId.GetNew();
            CalculatorE.RegisterTokenizer(CalculatorVariableAccessAddonE.VariableTokenizer);
            CalculatorE.RegisterPreprocessor(CalculatorVariableAccessAddonE.VariablePreprocessor);
        }

        public bool IsPowerOn()
        {
            if (_usePanelPowerFormula)
            {
                var panelFormulaResult = CalculatorE.ComputeFormula(Profile.PanelStorage.GetPanelById(_panelId).GetPowerFormula());
                if (panelFormulaResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.BooleanResult && panelFormulaResult.CalculatedBoolBoolValue == false)
                    return false;
            }
            var descriptorFormulaResult = CalculatorE.ComputeFormula(GetPowerFormula());
            if (descriptorFormulaResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.FormulaWasEmpty)
                return true;
            return descriptorFormulaResult.GetFormulaComputeResultType() == TypeOfComputeFormulaResult.BooleanResult &&
                   descriptorFormulaResult.CalculatedBoolBoolValue;
        }

        public void SetPowerFormula(string powerFormula)
        {
            if (_powerFormulaId == Guid.Empty)
                _powerFormulaId = GlobalFormulaKeeper.Instance.StoreFormula(powerFormula, GetId());
            else
                _powerFormulaId = GlobalFormulaKeeper.Instance.StoreOrChangeFormulaText(_powerFormulaId, powerFormula, GetId());
        }

        public string GetPowerFormula()
        {
            return _powerFormulaId == Guid.Empty ? null : GlobalFormulaKeeper.Instance.GetFormulaText(_powerFormulaId);
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
        public Guid GetId()
        {
            return Id;
        }
        public void SetId(Guid id)
        {
            Id = id;
        }

        public Guid GetAssignedPanelId()
        {
            return _panelId;
        }

        public void SetAssignedPanelId(Guid panelId)
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
            writer.WriteAttributeString("Id", Id.ToString());
            writer.WriteAttributeString("PanelId", _panelId.ToString());
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
            if (!Guid.TryParse(reader.GetAttribute("Id", reader.NamespaceURI), out Id))
            {
                // ToDo: удалить
                Id = GlobalId.GetByOldId(ObjType.AccessDescriptor, int.Parse(reader.GetAttribute("Id", reader.NamespaceURI)));
                if(Id == Guid.Empty)
                    Id = GlobalId.Register(ObjType.AccessDescriptor, int.Parse(reader.GetAttribute("Id", reader.NamespaceURI)));
            }
            GlobalFormulaKeeper.Instance.RemoveFormulasByOwnerId(GetId());

            if (!Guid.TryParse(reader.GetAttribute("PanelId", reader.NamespaceURI), out _panelId))
            {
                // ToDo: удалить
                _panelId = GlobalId.GetByOldId(ObjType.Panel, int.Parse(reader.GetAttribute("PanelId", reader.NamespaceURI)));
                if(_panelId == Guid.Empty)
                    _panelId = GlobalId.Register(ObjType.Panel, int.Parse(reader.GetAttribute("PanelId", reader.NamespaceURI)));
            }
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

        public virtual DescriptorBase GetCopy()
        {
            var item = (DescriptorBase)MemberwiseClone();
            return item;
        }

        public abstract string GetDescriptorType();
        public abstract System.Drawing.Bitmap GetIcon();
    }
}
