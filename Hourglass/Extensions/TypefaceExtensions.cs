using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Hourglass.Extensions;

/// <summary>
/// Holds DirectWrite font faces to keep their font files open.
/// </summary>
/// <remarks>
/// WPF releases all the DirectWrite font faces it has cached at the end of every layout pass. When a text changes
/// often, e.g. every second, DirectWrite reopens and remaps the font file every time the text is shaped again.
/// Holding a font face created from the same font file keeps the file open and prevents this I/O.
/// </remarks>
internal static class TypefaceExtensions
{
    private const int DWriteFontSimulationsNone = 0;

    private static IDWriteFactory? _factory;

    /// <param name="typeface">A <see cref="Typeface"/>.</param>
    extension(Typeface typeface)
    {
        /// <summary>
        /// Returns a handle holding the DirectWrite font face of the <see cref="Typeface"/>. The font file stays
        /// open as long as the handle is referenced; the font face is released when the handle is garbage collected.
        /// </summary>
        /// <returns>A handle holding the font face, or <c>null</c> if the font face could not be created.</returns>
        public object? HoldFontFace()
        {
            if (!typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface) || !glyphTypeface.FontUri.IsFile)
            {
                return null;
            }

            try
            {
                return CreateFontFace(glyphTypeface.FontUri);
            }
            catch
            {
                // Holding the font face is an optimization only.
                return null;
            }
        }
    }

    private static IDWriteFontFace? CreateFontFace(Uri fontUri)
    {
        // The shared factory is the one used by WPF, so DirectWrite reuses the font file opened here.
        IDWriteFactory factory = _factory ??= CreateFactory();

        factory.CreateFontFileReference(fontUri.LocalPath, IntPtr.Zero, out IDWriteFontFile fontFile);
        fontFile.Analyze(out bool isSupportedFontType, out _, out int fontFaceType, out _);

        if (!isSupportedFontType)
        {
            return null;
        }

        factory.CreateFontFace(fontFaceType, 1, [fontFile], GetFaceIndex(fontUri), DWriteFontSimulationsNone, out IDWriteFontFace fontFace);
        return fontFace;
    }

    private static IDWriteFactory CreateFactory()
    {
        Guid iid = typeof(IDWriteFactory).GUID;
        NativeMethods.DWriteCreateFactory(DWriteFactoryType.Shared, ref iid, out object factory);
        return (IDWriteFactory)factory;
    }

    /// <summary>
    /// Returns the index of the face in the font file, which WPF stores in the fragment of the font URI.
    /// </summary>
    /// <param name="fontUri">The font URI.</param>
    /// <returns>The index of the face in the font file.</returns>
    private static uint GetFaceIndex(Uri fontUri)
    {
        string fragment = fontUri.GetComponents(UriComponents.Fragment, UriFormat.SafeUnescaped);
        return uint.TryParse(fragment, NumberStyles.None, CultureInfo.InvariantCulture, out uint faceIndex) ? faceIndex : 0;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("B859EE5A-D838-4B5B-A2E8-1ADC7D93DB48")]
    private interface IDWriteFactory
    {
        void GetSystemFontCollection(out IntPtr fontCollection, [MarshalAs(UnmanagedType.Bool)] bool checkForUpdates);
        void CreateCustomFontCollection(IntPtr collectionLoader, IntPtr collectionKey, uint collectionKeySize, out IntPtr fontCollection);
        void RegisterFontCollectionLoader(IntPtr fontCollectionLoader);
        void UnregisterFontCollectionLoader(IntPtr fontCollectionLoader);
        void CreateFontFileReference([MarshalAs(UnmanagedType.LPWStr)] string filePath, IntPtr lastWriteTime, out IDWriteFontFile fontFile);
        void CreateCustomFontFileReference(IntPtr fontFileReferenceKey, uint fontFileReferenceKeySize, IntPtr fontFileLoader, out IDWriteFontFile fontFile);
        void CreateFontFace(int fontFaceType, uint numberOfFiles, [MarshalAs(UnmanagedType.LPArray)] IDWriteFontFile[] fontFiles, uint faceIndex, int fontFaceSimulationFlags, out IDWriteFontFace fontFace);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("739D886A-CEF5-47DC-8769-1A8B41BEBBB0")]
    private interface IDWriteFontFile
    {
        void GetReferenceKey(out IntPtr fontFileReferenceKey, out uint fontFileReferenceKeySize);
        void GetLoader(out IntPtr fontFileLoader);
        void Analyze([MarshalAs(UnmanagedType.Bool)] out bool isSupportedFontType, out int fontFileType, out int fontFaceType, out uint numberOfFaces);
    }

    /// <summary>
    /// Only held, never called, so no methods are declared.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5F49804D-7024-4D43-BFA9-D25984F53849")]
    private interface IDWriteFontFace
    {
    }
}
