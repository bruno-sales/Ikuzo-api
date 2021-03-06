﻿using System.Collections.Generic;
using Ikuzo.Domain.Entities;

namespace Ikuzo.Service.Interfaces
{
    public interface ILineService
    {
        Line Edit(Line line);
        void CreateLines(IEnumerable<Line> lines);
        IEnumerable<Line> GetAllLines();
        IEnumerable<Line> GetLocalLines(decimal latitude, decimal longitude, decimal variance);
        Line Get(string lineId); 
        Line Details(string lineId); 
    }
}
