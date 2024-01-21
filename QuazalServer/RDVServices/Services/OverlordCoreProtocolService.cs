﻿using QuazalServer.QNetZ.Attributes;
using QuazalServer.QNetZ;
using QuazalServer.QNetZ.Interfaces;

namespace QuazalServer.RDVServices.Services
{
    [RMCService(RMCProtocolId.OverlordCoreProtocolService)]
    public class OverlordCoreProtocolService : RMCServiceBase
    {
        [RMCMethod(1)]
        public RMCResult UKN1()
        {
            UNIMPLEMENTED();
            return Error(0);
        }
    }
}