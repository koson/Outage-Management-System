﻿namespace OMS.Web.Services.Commands
{
    public class IsolateOutageCommand : OutageLifecycleCommandBase
    {
        public IsolateOutageCommand(long gid): base(gid) { }
    }
}
