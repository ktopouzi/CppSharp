﻿using CppSharp.Parser.AST;
using System.Reflection;

namespace CppSharp.Parser
{
    public class ParserOptions : CppParserOptions
    {
        public ParserOptions()
        {
            Abi = Platform.IsUnixPlatform ? CppAbi.Itanium : CppAbi.Microsoft;
            MicrosoftMode = !Platform.IsUnixPlatform;
            CurrentDir = Assembly.GetExecutingAssembly().Location;
        }

        public bool IsItaniumLikeAbi { get { return Abi != CppAbi.Microsoft; } }
        public bool IsMicrosoftAbi { get { return Abi == CppAbi.Microsoft; } }
        public bool EnableRtti { get; set; }
        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.GNUPlusPlus11;

        /// Sets up the parser options to work with the given Visual Studio toolchain.
        public void SetupMSVC()
        {
            VisualStudioVersion vsVersion = VisualStudioVersion.Latest;

            // Silence "warning CS0162: Unreachable code detected"
            #pragma warning disable 162

            switch (BuildConfig.Choice)
            {
                case "vs2012":
                    vsVersion = VisualStudioVersion.VS2012;
                    break;
                case "vs2013":
                    vsVersion = VisualStudioVersion.VS2013;
                    break;
                case "vs2015":
                    vsVersion = VisualStudioVersion.VS2015;
                    break;
                case "vs2017":
                    vsVersion = VisualStudioVersion.VS2017;
                    break;

            #pragma warning restore 162

            }
            SetupMSVC(vsVersion);
        }

        public void SetupMSVC(VisualStudioVersion vsVersion)
        {
            MicrosoftMode = true;
            NoBuiltinIncludes = true;
            NoStandardIncludes = true;
            Abi = CppAbi.Microsoft;
            var clVersion = MSVCToolchain.GetCLVersion(vsVersion);
            ToolSetToUse = clVersion.Major * 10000000 + clVersion.Minor * 100000;

            AddArguments("-fms-extensions");
            AddArguments("-fms-compatibility");
            AddArguments("-fdelayed-template-parsing");

            var includes = MSVCToolchain.GetSystemIncludes(vsVersion);
            foreach (var include in includes)
                AddSystemIncludeDirs(include);
        }

        public void SetupXcode()
        {
            var builtinsPath = XcodeToolchain.GetXcodeBuiltinIncludesFolder();
            AddSystemIncludeDirs(builtinsPath);

            var cppIncPath = XcodeToolchain.GetXcodeCppIncludesFolder();
            AddSystemIncludeDirs(cppIncPath);

            var includePath = XcodeToolchain.GetXcodeIncludesFolder();
            AddSystemIncludeDirs(includePath);

            NoBuiltinIncludes = true;
            NoStandardIncludes = true;

            AddArguments("-stdlib=libc++");
        }

        public void Setup()
        {
            SetupArguments();
            SetupIncludes();
        }

        private void SetupArguments()
        {
            switch (LanguageVersion)
            {
                case LanguageVersion.C:
                case LanguageVersion.GNUC:
                    AddArguments("-xc");
                    break;
                default:
                    AddArguments("-xc++");
                    break;
            }

            switch (LanguageVersion)
            {
                case LanguageVersion.C:
                    AddArguments("-std=c99");
                    break;
                case LanguageVersion.GNUC:
                    AddArguments("-std=gnu99");
                    break;
                case LanguageVersion.CPlusPlus98:
                    AddArguments("-std=c++98");
                    break;
                case LanguageVersion.GNUPlusPlus98:
                    AddArguments("-std=gnu++98");
                    break;
                case LanguageVersion.CPlusPlus11:
                    AddArguments(MicrosoftMode ? "-std=c++14" : "-std=c++11");
                    break;
                default:
                    AddArguments(MicrosoftMode ? "-std=gnu++14" : "-std=gnu++11");
                    break;
            }

            if (!EnableRtti)
                AddArguments("-fno-rtti");
        }

        private void SetupIncludes()
        {
            if (Platform.IsMacOS)
                SetupXcode();
            else if (Platform.IsWindows && !NoBuiltinIncludes)
                SetupMSVC();
        }
    }

    public enum LanguageVersion
    {
        /**
        * The C programming language.
        */
        C,
        /**
        * The C programming language (GNU version).
        */
        GNUC,
        /**
        * The C++ programming language year 1998; supports deprecated constructs.
        */
        CPlusPlus98,
        /**
        * The C++ programming language year 1998; supports deprecated constructs (GNU version).
        */
        GNUPlusPlus98,
        /**
        * The C++ programming language year 2011.
        */
        CPlusPlus11,
        /**
        * The C++ programming language year 2011 (GNU version).
        */
        GNUPlusPlus11
    };
}
