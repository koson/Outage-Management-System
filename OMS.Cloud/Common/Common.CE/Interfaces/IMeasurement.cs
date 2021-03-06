﻿namespace Common.CE.Interfaces
{
    public interface IMeasurement : IGraphElement
    {
        long Id { get; set; }
        string Address { get; set; }
        bool IsInput { get; set; }
        long ElementId { get; set; }

        string GetMeasurementType();
        float GetCurrentValue();
    }
}
