//-----------------------------------------------------------------------
// <copyright file="RnfrCommandHandler.cs" company="Fubar Development Junker">
//     Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>
// <author>Mark Junker</author>
//-----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

using FubarDev.FtpServer.Features;
using FubarDev.FtpServer.FileSystem;

using JetBrains.Annotations;

namespace FubarDev.FtpServer.CommandHandlers
{
    /// <summary>
    /// Implements the <c>RNFR</c> command.
    /// </summary>
    public class RnfrCommandHandler : FtpCommandHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RnfrCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionAccessor">The accessor to get the connection that is active during the <see cref="Process"/> method execution.</param>
        public RnfrCommandHandler(IFtpConnectionAccessor connectionAccessor)
            : base(connectionAccessor, "RNFR")
        {
        }

        /// <inheritdoc/>
        public override bool IsAbortable => true;

        /// <inheritdoc/>
        public override async Task<IFtpResponse> Process(FtpCommand command, CancellationToken cancellationToken)
        {
            var fileName = command.Argument;
            var fsFeature = Connection.Features.Get<IFileSystemFeature>();
            var tempPath = fsFeature.Path.Clone();
            var fileInfo = await fsFeature.FileSystem.SearchEntryAsync(tempPath, fileName, cancellationToken).ConfigureAwait(false);
            if (fileInfo == null)
            {
                return new FtpResponse(550, T("Directory doesn't exist."));
            }

            if (fileInfo.Entry == null)
            {
                return new FtpResponse(550, T("Source entry doesn't exist."));
            }

            var renameFeature = new RenameFeature(fileInfo);
            Connection.Features.Set<IRenameCommandFeature>(renameFeature);

            var fullName = tempPath.GetFullPath(fileInfo.FileName);
            return new FtpResponse(350, T("Rename started ({0}).", fullName));
        }

        private class RenameFeature : IRenameCommandFeature
        {
            public RenameFeature([NotNull] SearchResult<IUnixFileSystemEntry> renameFrom)
            {
                RenameFrom = renameFrom;
            }

            /// <inheritdoc />
            public SearchResult<IUnixFileSystemEntry> RenameFrom { get; set; }
        }
    }
}
