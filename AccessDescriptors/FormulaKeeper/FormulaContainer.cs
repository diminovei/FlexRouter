using System;

namespace FlexRouter.AccessDescriptors.FormulaKeeper
{
    public class FormulaContainer
    {
        public string Formula;
        public Guid OwnerId;
        public Guid VariableId;
        public int ConnectorId;
        public FormulaContainer(string formula, Guid ownerId)
        {
            OwnerId = ownerId;
            Formula = formula;
            VariableId = Guid.Empty;
            ConnectorId = -1;
        }
        public FormulaContainer(string formula, Guid ownerId, Guid variableId, int connectorId)
        {
            OwnerId = ownerId;
            Formula = formula;
            VariableId = variableId;
            ConnectorId = connectorId;
        }
    }
}