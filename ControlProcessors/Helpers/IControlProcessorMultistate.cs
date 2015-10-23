using System.Collections.Generic;
using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.ControlProcessors.Helpers
{
    /// <summary>
    /// ��������� ��� ControlProcessor � ������� �����������
    /// </summary>
    public interface IControlProcessorMultistate
    {
        /// <summary>
        /// ������ ��������� ���������, ����� ������� ���������� ��� �������������� ���������, �������� ����� � �������� ������������
        /// </summary>
        /// <param name="states"></param>
        void RenewStatesInfo(IEnumerable<Connector> states);
    }
}