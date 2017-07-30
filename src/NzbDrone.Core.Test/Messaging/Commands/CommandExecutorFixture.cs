using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Messaging.Commands
{
    [TestFixture]
    public class CommandExecutorFixture : TestBase<CommandExecutor>
    {
        private BlockingCollection<CommandModel> _commandQueue;
        private Mock<IExecute<CommandA>> _executorA;
        private Mock<IExecute<CommandB>> _executorB;
        private List<CommandModel> _executedCommands;

        [SetUp]
        public void Setup()
        {
            _executorA = new Mock<IExecute<CommandA>>();
            _executorB = new Mock<IExecute<CommandB>>();
            _commandQueue = new BlockingCollection<CommandModel>(new CommandQueue());
            _executedCommands = new List<CommandModel>();

            Mocker.GetMock<IServiceFactory>()
                  .Setup(c => c.Build(typeof(IExecute<CommandA>)))
                  .Returns(_executorA.Object);

            Mocker.GetMock<IServiceFactory>()
                  .Setup(c => c.Build(typeof(IExecute<CommandB>)))
                  .Returns(_executorB.Object);

            Mocker.GetMock<IManageCommandQueue>()
                  .Setup(s => s.Queue(It.IsAny<CancellationToken>()))
                  .Returns(_commandQueue.GetConsumingEnumerable);

            Mocker.GetMock<IManageCommandQueue>()
                  .Setup(s => s.Complete(It.IsAny<CommandModel>(), It.IsAny<string>()))
                  .Callback<CommandModel, string>((command, completionMessage) => _executedCommands.Add(command));

            Mocker.GetMock<IManageCommandQueue>()
                  .Setup(s => s.Fail(It.IsAny<CommandModel>(), It.IsAny<string>(), It.IsAny<Exception>()))
                  .Callback<CommandModel, string, Exception>((command, completionMessage, exception) => _executedCommands.Add(command));
        }

        [TearDown]
        public void TearDown()
        {
            Subject.Handle(new ApplicationShutdownRequested());
        }

        private void WaitForExecution(CommandModel commandModel)
        {
            while (!_executedCommands.Any(c => c == commandModel))
            {
                Thread.Sleep(100);
            }
        }

        [Test]
        public void should_start_executor_threads()
        {
            Subject.Handle(new ApplicationStartedEvent());

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(v => v.Queue(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        }

        [Test]
        public void should_execute_on_executor()
        {
            var commandA = new CommandA();
            var commandModel = new CommandModel
                               {
                                   Body = commandA
                               };

            Subject.Handle(new ApplicationStartedEvent());
            _commandQueue.Add(commandModel);

            WaitForExecution(commandModel);

            _executorA.Verify(c => c.Execute(commandA), Times.Once());
        }

        [Test]
        public void should_not_execute_on_incompatible_executor()
        {
            var commandA = new CommandA();
            var commandModel = new CommandModel
            {
                Body = commandA
            };

            Subject.Handle(new ApplicationStartedEvent());
            _commandQueue.Add(commandModel);

            WaitForExecution(commandModel);

            _executorA.Verify(c => c.Execute(commandA), Times.Once());
            _executorB.Verify(c => c.Execute(It.IsAny<CommandB>()), Times.Never());
        }

        [Test]
        public void broken_executor_should_publish_executed_event()
        {
            var commandA = new CommandA();
            var commandModel = new CommandModel
            {
                Body = commandA
            };

            _executorA.Setup(s => s.Execute(It.IsAny<CommandA>()))
                      .Throws(new NotImplementedException());

            Subject.Handle(new ApplicationStartedEvent());
            _commandQueue.Add(commandModel);

            WaitForExecution(commandModel);

            VerifyEventPublished<CommandExecutedEvent>();
            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_publish_executed_event_on_success()
        {
            var commandA = new CommandA();
            var commandModel = new CommandModel
            {
                Body = commandA
            };

            Subject.Handle(new ApplicationStartedEvent());
            _commandQueue.Add(commandModel);

            WaitForExecution(commandModel);

            VerifyEventPublished<CommandExecutedEvent>();
        }

        [Test]
        public void should_use_completion_message()
        {
            var commandA = new CommandA();
            var commandModel = new CommandModel
            {
                Body = commandA
            };

            Subject.Handle(new ApplicationStartedEvent());
            _commandQueue.Add(commandModel);

            WaitForExecution(commandModel);

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(s => s.Complete(It.Is<CommandModel>(c => c == commandModel), commandA.CompletionMessage), Times.Once());
        }

        [Test]
        public void should_use_last_progress_message_if_completion_message_is_null()
        {
            var commandB = new CommandB();

            var commandModel = new CommandModel
            {
                Body = commandB,
                Message = "Do work"
            };

            Subject.Handle(new ApplicationStartedEvent());
            _commandQueue.Add(commandModel);

            WaitForExecution(commandModel);

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(s => s.Complete(It.Is<CommandModel>(c => c == commandModel), commandModel.Message), Times.Once());
        }
    }

    public class CommandA : Command
    {
        public CommandA(int id = 0)
        {
        }
    }

    public class CommandB : Command
    {

        public CommandB()
        {
            
        }

        public override string CompletionMessage => null;
    }

}