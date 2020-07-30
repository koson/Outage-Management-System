﻿using Common.OMS.OutageDatabaseModel;
using OMS.Common.Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OMS.ReportingServiceImplementation.Queries
{
    public class HistoricalConsumerOperationQuery : HistoricalConsumerSpecification
    {
        private readonly DatabaseOperation _operation;

        public HistoricalConsumerOperationQuery(DatabaseOperation operation)
            => _operation = operation;

        public override Expression<Func<ConsumerHistorical, bool>> IsSatisfiedBy => x => x.DatabaseOperation == _operation;
    }
}
