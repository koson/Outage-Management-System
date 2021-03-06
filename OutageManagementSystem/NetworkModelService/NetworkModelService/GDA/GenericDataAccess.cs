﻿using Outage.Common;
using Outage.Common.ServiceContracts.GDA;
using Outage.Common.GDA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outage.NetworkModelService.GDA
{
    public class GenericDataAccess : INetworkModelGDAContract
    {
        private ILogger logger;

        protected ILogger Logger
        {
            get { return logger ?? (logger = LoggerWrapper.Instance); }
        }

        private static Dictionary<int, ResourceIterator> resourceIterators = new Dictionary<int, ResourceIterator>();
        private static int resourceItId = 0;

        protected static NetworkModel networkModel = null;

        public static NetworkModel NetworkModel
        {
            set
            {
                networkModel = value;
            }
        }

        public GenericDataAccess()
        {
        }

        public UpdateResult ApplyUpdate(Delta delta)
        {
            return networkModel.ApplyDelta(delta);
        }

        public ResourceDescription GetValues(long resourceId, List<ModelCode> propIds)
        {
            try
            {
                ResourceDescription retVal = networkModel.GetValues(resourceId, propIds);
                return retVal;
            }
            catch (Exception ex)
            {
                string message = string.Format("Getting values for resource with ID: 0x{0:X16} failed. {1}", resourceId, ex.Message);
                Logger.LogError(message, ex);
                throw new Exception(message);
            }
        }

        public int GetExtentValues(ModelCode entityType, List<ModelCode> propIds)
        {
            try
            {
                ResourceIterator ri = networkModel.GetExtentValues(entityType, propIds);
                int retVal = AddIterator(ri);

                return retVal;
            }
            catch (Exception ex)
            {
                string message = string.Format("Getting extent values for ModelCode = {0} failed. ", entityType, ex.Message);
                Logger.LogError(message, ex);
                throw new Exception(message);
            }
        }

        public int GetRelatedValues(long source, List<ModelCode> propIds, Association association)
        {
            try
            {
                ResourceIterator ri = networkModel.GetRelatedValues(source, propIds, association);
                int retVal = AddIterator(ri);

                return retVal;
            }
            catch (Exception ex)
            {
                string message = string.Format("Getting related values for resource with ID: 0x{0:X16} failed. {1}", source, ex.Message);
                Logger.LogError(message, ex);
                throw new Exception(message);
            }
        }

        public List<ResourceDescription> IteratorNext(int n, int id)
        {
            try
            {
                List<ResourceDescription> retVal = GetIterator(id).Next(n);

                return retVal;
            }
            catch (Exception ex)
            {
                string message = string.Format("IteratorNext failed. Iterator ID: {0}. Resources to fetch count = {1}. {2} ", id, n, ex.Message);
                Logger.LogError(message, ex);
                throw new Exception(message);
            }
        }

        public bool IteratorRewind(int id)
        {
            try
            {
                GetIterator(id).Rewind();

                return true;
            }
            catch (Exception ex)
            {
                string message = string.Format("IteratorRewind failed. Iterator ID: {0}. {1}", id, ex.Message);
                Logger.LogError(message, ex);
                throw new Exception(message);
            }
        }

        public int IteratorResourcesTotal(int id)
        {
            try
            {
                int retVal = GetIterator(id).ResourcesTotal();
                return retVal;
            }
            catch (Exception ex)
            {
                string message = string.Format("IteratorResourcesTotal failed. Iterator ID: {0}. {1}", id, ex.Message);
                Logger.LogError(message, ex);
                throw new Exception(message);
            }
        }

        public int IteratorResourcesLeft(int id)
        {
            try
            {
                int resourcesLeft = GetIterator(id).ResourcesLeft();

                return resourcesLeft;
            }
            catch (Exception ex)
            {
                string message = string.Format("IteratorResourcesLeft failed. Iterator ID: {0}. {1}", id, ex.Message);
                Logger.LogError(message, ex);
                throw new Exception(message);
            }
        }

        public bool IteratorClose(int id)
        {
            try
            {
                bool retVal = RemoveIterator(id);

                return retVal;
            }
            catch (Exception ex)
            {
                string message = string.Format("IteratorClose failed. Iterator ID: {0}. {1}", id, ex.Message);
                Logger.LogError(message, ex);
                throw new Exception(message);
            }
        }

        private int AddIterator(ResourceIterator iterator)
        {
            lock (resourceIterators)
            {
                int iteratorId = ++resourceItId;
                resourceIterators.Add(iteratorId, iterator);
                return iteratorId;
            }
        }

        private ResourceIterator GetIterator(int iteratorId)
        {
            lock (resourceIterators)
            {
                if (resourceIterators.ContainsKey(iteratorId))
                {
                    return resourceIterators[iteratorId];
                }
                else
                {
                    throw new Exception(string.Format("Iterator with given ID: {0} doesn't exist.", iteratorId));
                }
            }
        }

        private bool RemoveIterator(int iteratorId)
        {
            lock (resourceIterators)
            {
                return resourceIterators.Remove(iteratorId);
            }
        }
    }
}
