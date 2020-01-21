﻿using System.Collections.Generic;

namespace CECommon
{
	public abstract class TopologyElement
	{
        #region Fields
        private long id;
		private TopologyElement firstEnd;
		private List<TopologyElement> secondEnd;
		private string dmsType;
		#endregion

		#region Properties
		public long Id { get => id; set => id = value; }
		public TopologyElement FirstEnd { get => firstEnd; set => firstEnd = value; }
		public List<TopologyElement> SecondEnd { get => secondEnd; set => secondEnd = value; }
		public string DmsType { get => dmsType; set => dmsType = value; }

        #endregion
        public TopologyElement(long gid)
		{
			Id = gid;
			SecondEnd = new List<TopologyElement>();
		}
	}
}
