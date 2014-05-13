namespace FlexRouter.AccessDescriptors.Interfaces
{
    interface IDescriptorRangeExt
    {
        /// <summary>
        /// Установить "позицию". Вроде процента с положительными и отрицательными значениями.
        /// </summary>
        /// <param name="positionPercentage">Позиция бегунка в процентах</param>
        void SetPositionInPercents(double positionPercentage);
    }
}
