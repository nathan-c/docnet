﻿using System.Collections.Generic;
using Docnet.Core.Bindings;
using Docnet.Core.Editors;
using Docnet.Core.Readers;
using Docnet.Core.Validation;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Docnet.Core
{
    public sealed class DocLib : IDocLib
    {
        /// <summary>
        /// PDFium is not thread-safe
        /// so we need to lock every native
        /// call. We might implement
        /// Command patter or something similar
        /// to get around this in the future.
        /// </summary>
        internal static readonly object Lock = new object();

        private static DocLib _instance;

        private readonly IDocEditor _editor;

        private DocLib()
        {
            fpdf_view.FPDF_InitLibrary();

            _editor = new DocEditor();
        }

        public static DocLib Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DocLib();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <inheritdoc />
        public IDocReader GetDocReader(string filePath, int dimOne, int dimTwo)
        {
            return GetDocReader(filePath, null, dimOne, dimTwo);
        }

        /// <inheritdoc />
        public IDocReader GetDocReader(string filePath, string password, int dimOne, int dimTwo)
        {
            Validator.CheckFilePathNotNull(filePath, nameof(filePath));

            Validator.CheckNotLessOrEqualToZero(dimOne, nameof(dimOne));
            Validator.CheckNotLessOrEqualToZero(dimTwo, nameof(dimTwo));

            Validator.CheckNotGreaterThan(dimOne, dimTwo, nameof(dimOne), nameof(dimTwo));

            return new DocReader(filePath, password, dimOne, dimTwo);
        }

        /// <inheritdoc />
        public IDocReader GetDocReader(byte[] bytes, int dimOne, int dimTwo)
        {
            return GetDocReader(bytes, null, dimOne, dimTwo);
        }

        /// <inheritdoc />
        public IDocReader GetDocReader(byte[] bytes, string password, int dimOne, int dimTwo)
        {
            Validator.CheckBytesNullOrZero(bytes, nameof(bytes));

            Validator.CheckNotLessOrEqualToZero(dimOne, nameof(dimOne));
            Validator.CheckNotLessOrEqualToZero(dimTwo, nameof(dimTwo));

            Validator.CheckNotGreaterThan(dimOne, dimTwo, nameof(dimOne), nameof(dimTwo));

            return new DocReader(bytes, password, dimOne, dimTwo);
        }

        /// <inheritdoc />
        public byte[] Merge(string fileOne, string fileTwo)
        {
            Validator.CheckFilePathNotNull(fileOne, nameof(fileOne));
            Validator.CheckFilePathNotNull(fileTwo, nameof(fileTwo));

            return _editor.Merge(fileOne, fileTwo);
        }

        /// <inheritdoc />
        public byte[] Merge(byte[] fileOne, byte[] fileTwo)
        {
            Validator.CheckBytesNullOrZero(fileOne, nameof(fileOne));
            Validator.CheckBytesNullOrZero(fileTwo, nameof(fileTwo));

            return _editor.Merge(fileOne, fileTwo);
        }

        /// <inheritdoc />
        public byte[] Split(string filePath, int pageFromIndex, int pageToIndex)
        {
            Validator.CheckFilePathNotNull(filePath, nameof(filePath));

            Validator.CheckNotLessThanZero(pageFromIndex, nameof(pageFromIndex));
            Validator.CheckNotLessThanZero(pageToIndex, nameof(pageToIndex));

            Validator.CheckNotGreaterThan(pageFromIndex, pageToIndex, nameof(pageFromIndex), nameof(pageToIndex));

            return _editor.Split(filePath, pageFromIndex, pageToIndex);
        }

        /// <inheritdoc />
        public byte[] Split(byte[] bytes, int pageFromIndex, int pageToIndex)
        {
            Validator.CheckBytesNullOrZero(bytes, nameof(bytes));

            Validator.CheckNotLessThanZero(pageFromIndex, nameof(pageFromIndex));
            Validator.CheckNotLessThanZero(pageToIndex, nameof(pageToIndex));

            Validator.CheckNotGreaterThan(pageFromIndex, pageToIndex, nameof(pageFromIndex), nameof(pageToIndex));

            return _editor.Split(bytes, pageFromIndex, pageToIndex);
        }

        /// <inheritdoc />
        public byte[] Unlock(string filePath, string password)
        {
            Validator.CheckFilePathNotNull(filePath, nameof(filePath));

            return _editor.Unlock(filePath, password);
        }

        /// <inheritdoc />
        public byte[] Unlock(byte[] bytes, string password)
        {
            Validator.CheckBytesNullOrZero(bytes, nameof(bytes));

            return _editor.Unlock(bytes, password);
        }

        public byte[] JpegToPdf(IReadOnlyList<JpegImage> files)
        {
            foreach (var jpegImage in files)
            {
                Validator.CheckBytesNullOrZero(jpegImage.Bytes, nameof(jpegImage.Bytes));

                Validator.CheckNotLessThanZero(jpegImage.Width, nameof(jpegImage.Width));
                Validator.CheckNotLessThanZero(jpegImage.Height, nameof(jpegImage.Height));
            }

            return _editor.JpegToPdf(files);
        }

        public string GetLastError()
        {
            lock (Lock)
            {
                var code = fpdf_view.FPDF_GetLastError();

                switch (code)
                {
                    case 0:
                        return "no error";
                    case 1:
                        return "unknown error";
                    case 2:
                        return "file not found or could not be opened";
                    case 3:
                        return "file not in PDF format or corrupted";
                    case 4:
                        return "password required or incorrect password";
                    case 5:
                        return "unsupported security scheme";
                    case 6:
                        return "page not found or content error";
                    case 1001:
                        return "the requested operation cannot be completed due to a license restrictions";
                    default:
                        return "unknown error";
                }
            }
        }

        public void Dispose()
        {
            lock (Lock)
            {
                fpdf_view.FPDF_DestroyLibrary();
            }

            _instance = null;
        }
    }
}
