using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NewLaserProject.Classes.Mediator
{
    internal class InfoMessageRequest:IRequest
    {
        public string Message { get; set; }

    }

    internal class InfoMessageHandler : IRequestHandler<InfoMessageRequest>
    {
        public Task<Unit> Handle(InfoMessageRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Unit());
            //throw new NotImplementedException();
        }
    }
}
