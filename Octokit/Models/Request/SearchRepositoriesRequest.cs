using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Octokit.Internal;

namespace Octokit
{
    /// <summary>
    /// Searching Repositories
    /// http://developer.github.com/v3/search/#search-repositories
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class SearchRepositoriesRequest : BaseSearchRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchRepositoriesRequest"/> class.
        /// </summary>
        public SearchRepositoriesRequest()
        {
            Order = SortDirection.Descending;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchRepositoriesRequest"/> class.
        /// </summary>
        /// <param name="term">The search term.</param>
        public SearchRepositoriesRequest(string term)
            : base(term)
        {
            Order = SortDirection.Descending;
        }

        /// <summary>
        /// For https://help.github.com/articles/searching-repositories#sorting
        /// Optional Sort field. One of stars, forks, or updated. If not provided, results are sorted by best match.
        /// </summary>
        public RepoSearchSort? SortField { get; set; }

        public override string Sort
        {
            get { return SortField.ToParameter(); }
        }

        private IEnumerable<InQualifier> _inQualifier;

        /// <summary>
        /// The in qualifier limits what fields are searched. With this qualifier you can restrict the search to just the repository name, description, README, or any combination of these. 
        /// Without the qualifier, only the name and description are searched.
        /// https://help.github.com/articles/searching-repositories#search-in
        /// </summary>
        public IEnumerable<InQualifier> In
        {
            get
            {
                return _inQualifier;
            }
            set
            {
                if (value != null && value.Any())
                    _inQualifier = value.Distinct().ToList();
            }
        }

        /// <summary>
        /// Filters repositories based on the number of forks, and/or whether forked repositories should be included in the results at all.
        /// https://help.github.com/articles/searching-repositories#forks
        /// </summary>
        public Range Forks { get; set; }

        /// <summary>
        /// Filters repositories based whether forked repositories should be included in the results at all.
        /// Defaults to ExcludeForks
        /// https://help.github.com/articles/searching-repositories#forks
        /// </summary>
        public ForkQualifier? Fork { get; set; }

        /// <summary>
        /// The size qualifier finds repository's that match a certain size (in kilobytes).
        /// https://help.github.com/articles/searching-repositories#size
        /// </summary>
        public Range Size { get; set; }

        /// <summary>
        /// Searches repositories based on the language they’re written in.
        /// https://help.github.com/articles/searching-repositories#languages
        /// </summary>
        public StringEnum<Language>? Language { get; set; }

        /// <summary>
        /// Searches repositories based on the number of stars.
        /// https://help.github.com/articles/searching-repositories#stars
        /// </summary>
        public Range Stars { get; set; }

        /// <summary>
        /// Limits searches to a specific user or repository.
        /// https://help.github.com/articles/searching-repositories#users-organizations-and-repositories
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Filters repositories based on times of creation.
        /// https://help.github.com/articles/searching-repositories#created-and-last-updated
        /// </summary>
        public DateRange Created { get; set; }

        /// <summary>
        /// Filters repositories based on when they were last updated.
        /// https://help.github.com/articles/searching-repositories#created-and-last-updated
        /// </summary>
        public DateRange Updated { get; set; }

        public override IReadOnlyList<string> MergedQualifiers()
        {
            var parameters = new List<string>();

            if (In != null)
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "in:{0}", string.Join(",", In)));
            }

            if (Size != null)
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "size:{0}", Size));
            }

            if (Forks != null)
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "forks:{0}", Forks));
            }

            if (Fork != null)
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "fork:{0}", Fork));
            }

            if (Stars != null)
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "stars:{0}", Stars));
            }

            if (Language.HasValue && !string.IsNullOrWhiteSpace(Language.Value.StringValue))
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "language:{0}", Language.Value.StringValue));
            }

            if (User.IsNotBlank())
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "user:{0}", User));
            }

            if (Created != null)
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "created:{0}", Created));
            }

            if (Updated != null)
            {
                parameters.Add(string.Format(CultureInfo.InvariantCulture, "pushed:{0}", Updated));
            }
            return parameters;
        }

        internal string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "Term: {0} Sort: {1}", Term, Sort);
            }
        }
    }

    /// <summary>
    /// https://help.github.com/articles/searching-repositories#search-in
    /// The in qualifier limits what fields are searched. With this qualifier you can restrict the search to just the 
    /// repository name, description, README, or any combination of these.
    /// </summary>
    public enum InQualifier
    {
        [Parameter(Value = "name")]
        Name,

        [Parameter(Value = "description")]
        Description,

        [Parameter(Value = "readme")]
        Readme
    }

    /// <summary>
    /// Helper class in generating the range values for a qualifer e.g. In or Size qualifiers
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Range
    {
        private readonly string query = string.Empty;

        /// <summary>
        /// Matches repositories that are <param name="size">size</param> MB exactly
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
        public Range(int size)
        {
            query = size.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Matches repositories that are between <param name="minSize"/> and <param name="maxSize"/> KB
        /// </summary>
        public Range(int minSize, int maxSize)
        {
            query = string.Format(CultureInfo.InvariantCulture, "{0}..{1}", minSize, maxSize);
        }

        /// <summary>
        /// Matches repositories with regards to the size <param name="size"/> 
        /// We will use the <param name="op"/> to see what operator will be applied to the size qualifier
        /// </summary>
        public Range(int size, SearchQualifierOperator op)
        {
            switch (op)
            {
                case SearchQualifierOperator.GreaterThan:
                    query = string.Format(CultureInfo.InvariantCulture, ">{0}", size);
                    break;
                case SearchQualifierOperator.LessThan:
                    query = string.Format(CultureInfo.InvariantCulture, "<{0}", size);
                    break;
                case SearchQualifierOperator.LessThanOrEqualTo:
                    query = string.Format(CultureInfo.InvariantCulture, "<={0}", size);
                    break;
                case SearchQualifierOperator.GreaterThanOrEqualTo:
                    query = string.Format(CultureInfo.InvariantCulture, ">={0}", size);
                    break;
            }
        }

        internal string DebuggerDisplay
        {
            get { return string.Format(CultureInfo.InvariantCulture, "Query: {0}", query); }
        }

        /// <summary>
        /// Helper class that build a <see cref="Range"/> with a LessThan comparator used for filtering results
        /// </summary>
        public static Range LessThan(int size)
        {
            return new Range(size, SearchQualifierOperator.LessThan);
        }

        /// <summary>
        /// Helper class that build a <see cref="Range"/> with a LessThanOrEqual comparator used for filtering results
        /// </summary>
        public static Range LessThanOrEquals(int size)
        {
            return new Range(size, SearchQualifierOperator.LessThanOrEqualTo);
        }

        /// <summary>
        /// Helper class that build a <see cref="Range"/> with a GreaterThan comparator used for filtering results
        /// </summary>
        public static Range GreaterThan(int size)
        {
            return new Range(size, SearchQualifierOperator.GreaterThan);
        }

        /// <summary>
        /// Helper class that build a <see cref="Range"/> with a GreaterThanOrEqualTo comparator used for filtering results
        /// </summary>
        public static Range GreaterThanOrEquals(int size)
        {
            return new Range(size, SearchQualifierOperator.GreaterThanOrEqualTo);
        }

        public override string ToString()
        {
            return query;
        }
    }

    /// <summary>
    /// helper class in generating the date range values for the date qualifier e.g.
    /// https://help.github.com/articles/searching-repositories#created-and-last-updated
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class DateRange
    {
        private readonly string query = string.Empty;

        /// <summary>
        /// Matches repositories with regards to the <param name="date"/>.
        /// We will use the <param name="op"/> to see what operator will be applied to the date qualifier
        /// </summary>
        public DateRange(DateTime date, SearchQualifierOperator op)
        {
            switch (op)
            {
                case SearchQualifierOperator.GreaterThan:
                    query = string.Format(CultureInfo.InvariantCulture, ">{0:yyyy-MM-dd}", date);
                    break;
                case SearchQualifierOperator.LessThan:
                    query = string.Format(CultureInfo.InvariantCulture, "<{0:yyyy-MM-dd}", date);
                    break;
                case SearchQualifierOperator.LessThanOrEqualTo:
                    query = string.Format(CultureInfo.InvariantCulture, "<={0:yyyy-MM-dd}", date);
                    break;
                case SearchQualifierOperator.GreaterThanOrEqualTo:
                    query = string.Format(CultureInfo.InvariantCulture, ">={0:yyyy-MM-dd}", date);
                    break;
            }
        }

        /// <summary>
        /// Matches repositories with regards to both the <param name="from"/> and <param name="to"/> dates.
        /// </summary>
        public DateRange(DateTime from, DateTime to)
        {
            query = string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd}..{1:yyyy-MM-dd}", from, to);
        }

        internal string DebuggerDisplay
        {
            get { return string.Format(CultureInfo.InvariantCulture, "Query: {0}", query); }
        }

        /// <summary>
        /// helper method to create a LessThan Date Comparison
        /// e.g. &lt; 2011
        /// </summary>
        /// <param name="date">date to be used for comparison (times are ignored)</param>
        /// <returns><see cref="DateRange"/></returns>
        public static DateRange LessThan(DateTime date)
        {
            return new DateRange(date, SearchQualifierOperator.LessThan);
        }

        /// <summary>
        /// helper method to create a LessThanOrEqualTo Date Comparison
        /// e.g. &lt;= 2011
        /// </summary>
        /// <param name="date">date to be used for comparison (times are ignored)</param>
        /// <returns><see cref="DateRange"/></returns>
        public static DateRange LessThanOrEquals(DateTime date)
        {
            return new DateRange(date, SearchQualifierOperator.LessThanOrEqualTo);
        }

        /// <summary>
        /// helper method to create a GreaterThan Date Comparison
        /// e.g. > 2011
        /// </summary>
        /// <param name="date">date to be used for comparison (times are ignored)</param>
        /// <returns><see cref="DateRange"/></returns>
        public static DateRange GreaterThan(DateTime date)
        {
            return new DateRange(date, SearchQualifierOperator.GreaterThan);
        }

        /// <summary>
        /// helper method to create a GreaterThanOrEqualTo Date Comparison
        /// e.g. >= 2011
        /// </summary>
        /// <param name="date">date to be used for comparison (times are ignored)</param>
        /// <returns><see cref="DateRange"/></returns>
        public static DateRange GreaterThanOrEquals(DateTime date)
        {
            return new DateRange(date, SearchQualifierOperator.GreaterThanOrEqualTo);
        }

        /// <summary>
        /// helper method to create a bounded Date Comparison
        /// e.g. 2015-08-01..2015-10-31
        /// </summary>
        /// <param name="from">earlier date of the two</param>
        /// <param name="to">latter date of the two</param>
        /// <returns><see cref="DateRange"/></returns>
        public static DateRange Between(DateTime from, DateTime to)
        {
            return new DateRange(from, to);
        }

        public override string ToString()
        {
            return query;
        }
    }

    /// <summary>
    /// lanuages that can be searched on in github
    /// https://help.github.com/articles/searching-repositories#languages
    /// </summary>
    public enum Language
    {
#pragma warning disable 1591
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Abap")]
        [Parameter(Value = "abap")]
        Abap,

        [Parameter(Value = "ActionScript")]
        ActionScript,
        
        [Parameter(Value = "ada")]
        Ada,
        
        [Parameter(Value = "apex")]
        Apex,
        
        [Parameter(Value = "AppleScript")]
        AppleScript,
        
        [Parameter(Value = "arc")]
        Arc,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Arduino")]
        [Parameter(Value = "arduino")]
        Arduino,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Conf")]
        [Parameter(Value = "ApacheConf")]
        ApacheConf,
        
        [Parameter(Value = "asp")]
        Asp,

        [Parameter(Value = "assembly")]
        Assembly,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Augeas")]
        [Parameter(Value = "augeas")]
        Augeas,

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "HotKey")]
        [Parameter(Value = "AutoHotkey")]
        AutoHotKey,
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Awk")]
        [Parameter(Value = "awk")]
        Awk,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Batchfile")]
        [Parameter(Value = "batchfile")]
        Batchfile,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Befunge")]
        [Parameter(Value = "befunge")]
        Befunge,

        [Parameter(Value = "BlitzMax")]
        BlitzMax,

        [Parameter(Value = "boo")]
        Boo,

        [Parameter(Value = "bro")]
        Bro,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "C")]
        [Parameter(Value = "c")]
        C,

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "hs")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "hs")]
        [Parameter(Value = "C2HS Haskell")]
        C2hsHaskell,
        
        [Parameter(Value = "ceylon")]
        Ceylon,

        [Parameter(Value = "chuck")]
        Chuck,

        [Parameter(Value = "clips")]
        Clips,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Clojure")]
        [Parameter(Value = "clojure")]
        Clojure,

        [Parameter(Value = "cobol")]
        Cobol,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cmake")]
        [Parameter(Value = "cmake")]
        Cmake,

        [Parameter(Value = "C-ObjDump")]
        CObjDump,

        [Parameter(Value = "CoffeeScript")]
        CoffeeScript,
        
        [Parameter(Value = "ColdFusion")]
        ColdFusion,
        
        [Parameter(Value = "commonlisp")]
        CommonLisp,

        [Parameter(Value = "coq")]
        Coq,

        [Parameter(Value = "C++")]
        CPlusPlus,
        
        [Parameter(Value = "CSharp")]
        CSharp,
        
        [Parameter(Value = "css")]
        Css,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cpp")]
        [Parameter(Value = "Cpp-ObjDump")]
        CppObjDump,
        
        [Parameter(Value = "cucumber")]
        Cucumber,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cython")]
        [Parameter(Value = "cython")]
        Cython,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "D")]
        [Parameter(Value = "d")]
        D,

        [Parameter(Value = "D-ObjDump")]
        DObjDump,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Darcs")]
        [Parameter(Value = "DarcsPatch")]
        DarcsPatch,

        [Parameter(Value = "dart")]
        Dart,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dcpu")]
        [Parameter(Value = "DCPU-16 ASM")]
        Dcpu16Asm,

        [Parameter(Value = "dot")]
        Dot,

        [Parameter(Value = "dylan")]
        Dylan,

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ec")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ec")]
        [Parameter(Value = "ec")]
        Ec,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ecere")]
        [Parameter(Value = "Ecere Projects")]
        EcereProjects,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ecl")]
        [Parameter(Value = "ecl")]
        Ecl,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Edn")]
        [Parameter(Value = "edn")]
        Edn,

        [Parameter(Value = "eiffel")]
        Eiffel,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Elixir")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Elixir")]
        [Parameter(Value = "elixir")]
        Elixir,

        [Parameter(Value = "elm")]
        Elm,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Emacs")]
        [Parameter(Value = "emacslisp")]
        EmacsLisp,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Erlang")]
        [Parameter(Value = "erlang")]
        Erlang,

        [Parameter(Value = "F#")]
        FSharp,

        [Parameter(Value = "factor")]
        Factor,

        [Parameter(Value = "fancy")]
        Fancy,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Fantom")]
        [Parameter(Value = "fantom")]
        Fantom,

        [Parameter(Value = "fish")]
        Fish,

        [Parameter(Value = "forth")]
        Forth,

        [Parameter(Value = "fortran")]
        Fortran,

        [Parameter(Value = "gas")]
        Gas,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Genshi")]
        [Parameter(Value = "genshi")]
        Genshi,

        [Parameter(Value = "Gentoo Build")]
        GentooBuild,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Eclass")]
        [Parameter(Value = "Gentoo Eclass")]
        GentooEclass,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Gettext")]
        [Parameter(Value = "Gettext Catalog")]
        GettextCatalog,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Glsl")]
        [Parameter(Value = "glsl")]
        Glsl,

        [Parameter(Value = "go")]
        Go,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Gosu")]
        [Parameter(Value = "su")]
        su,

        [Parameter(Value = "groff")]
        Groff,

        [Parameter(Value = "groovy")]
        Groovy,

        [Parameter(Value = "Groovy Server Pages")]
        GroovyServerPages,
        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Haml")]
        [Parameter(Value = "haml")]
        Haml,

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "HandleBars")]
        [Parameter(Value = "HandleBars")]
        HandleBars,

        [Parameter(Value = "haskell")]
        Haskell,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Haxe")]
        [Parameter(Value = "haxe")]
        Haxe,

        [Parameter(Value = "http")]
        Http,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ini")]
        [Parameter(Value = "ini")]
        Ini,

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Io")]
        [Parameter(Value = "io")]
        Io,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ioke")]
        [Parameter(Value = "ioke")]
        Ioke,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Irc")]
        [Parameter(Value = "IRC log")]
        IrcLog,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "J")]
        [Parameter(Value = "j")]
        J,

        [Parameter(Value = "java")]
        Java,

        [Parameter(Value = "Java Server Pages")]
        JavaServerPages,

        [Parameter(Value = "javascript")]
        JavaScript,

        [Parameter(Value = "json")]
        Json,

        [Parameter(Value = "julia")]
        Julia,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Kotlin")]
        [Parameter(Value = "kotlin")]
        Kotlin,

        [Parameter(Value = "lasso")]
        Lasso,

        [Parameter(Value = "less")]
        Less,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lfe")]
        [Parameter(Value = "lfe")]
        Lfe,

        [Parameter(Value = "LillyPond")]
        LillyPond,

        [Parameter(Value = "Literate CoffeeScript")]
        LiterateCoffeeScript,

        [Parameter(Value = "Literate Haskell")]
        LiterateHaskell,

        [Parameter(Value = "LiveScript")]
        LiveScript,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Llvm")]
        [Parameter(Value = "llvm")]
        Llvm,

        [Parameter(Value = "logos")]
        Logos,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Logtalk")]
        [Parameter(Value = "logtalk")]
        Logtalk,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lua")]
        [Parameter(Value = "lua")]
        Lua,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "M")]
        [Parameter(Value = "m")]
        M,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Makefile")]
        [Parameter(Value = "makefile")]
        Makefile,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mako")]
        [Parameter(Value = "mako")]
        Mako,

        [Parameter(Value = "markdown")]
        Markdown,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Matlab")]
        [Parameter(Value = "matlab")]
        Matlab,

        [Parameter(Value = "max")]
        Max,

        [Parameter(Value = "MiniD")]
        MiniD,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mirah")]
        [Parameter(Value = "mirah")]
        Mirah,

        [Parameter(Value = "monkey")]
        Monkey,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Moocode")]
        [Parameter(Value = "moocode")]
        Moocode,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Moonscript")]
        [Parameter(Value = "moonscript")]
        Moonscript,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mupad")]
        [Parameter(Value = "mupad")]
        Mupad,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Myghty")]
        [Parameter(Value = "myghty")]
        Myghty,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nemerle")]
        [Parameter(Value = "nemerle")]
        Nemerle,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nginx")]
        [Parameter(Value = "nginx")]
        Nginx,

        [Parameter(Value = "nimrod")]
        Nimrod,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Nsis")]
        [Parameter(Value = "nsis")]
        Nsis,

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Nu")]
        [Parameter(Value = "nu")]
        Nu,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Num")]
        [Parameter(Value = "NumPY")]
        NumPY,

        [Parameter(Value = "ObjDump")]
        ObjDump,

        [Parameter(Value = "objectivec")]
        ObjectiveC,

        [Parameter(Value = "objectivej")]
        ObjectiveJ,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Caml")]
        [Parameter(Value = "OCaml")]
        OCaml,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Omgrofl")]
        [Parameter(Value = "omgrofl")]
        Omgrofl,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ooc")]
        [Parameter(Value = "ooc")]
        Ooc,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Opa")]
        [Parameter(Value = "opa")]
        Opa,

        [Parameter(Value = "OpenCL")]
        OpenCL,
        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Abl")]
        [Parameter(Value = "OpenEdge ABL")]
        OpenEdgeAbl,

        [Parameter(Value = "parrot")]
        Parrot,

        [Parameter(Value = "Parrot Assembly")]
        ParrotAssembly,

        [Parameter(Value = "Parrot Internal Representation")]
        ParrotInternalRepresentation,

        [Parameter(Value = "pascal")]
        Pascal,

        [Parameter(Value = "perl")]
        Perl,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Php")]
        [Parameter(Value = "php")]
        Php,

        [Parameter(Value = "pike")]
        Pike,

        [Parameter(Value = "PogoScript")]
        PogoScript,

        [Parameter(Value = "PowerShell")]
        PowerShell,
        
        [Parameter(Value = "processing")]
        Processing,

        [Parameter(Value = "prolog")]
        Prolog,

        [Parameter(Value = "puppet")]
        Puppet,

        [Parameter(Value = "Pure Data")]
        PureData,

        [Parameter(Value = "python")]
        Python,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Traceback")]
        [Parameter(Value = "Python traceback")]
        PythonTraceback,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "R")]
        [Parameter(Value = "r")]
        R,

        [Parameter(Value = "racket")]
        Racket,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ragel")]
        [Parameter(Value = "Ragel in Ruby Host")]
        RagelInRubyHost,

        [Parameter(Value = "Raw token data")]
        RawTokenData,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Rebol")]
        [Parameter(Value = "rebol")]
        Rebol,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Redcode")]
        [Parameter(Value = "redcode")]
        Redcode,

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ReStructured")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Re")]
        [Parameter(Value = "reStructuredText")]
        ReStructuredText,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Rhtml")]
        [Parameter(Value = "rhtml")]
        Rhtml,

        [Parameter(Value = "rouge")]
        Rouge,

        [Parameter(Value = "ruby")]
        Ruby,

        [Parameter(Value = "rust")]
        Rust,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Scala")]
        [Parameter(Value = "scala")]
        Scala,

        [Parameter(Value = "scheme")]
        Scheme,

        [Parameter(Value = "sage")]
        Sage,

        [Parameter(Value = "sass")]
        Sass,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Scilab")]
        [Parameter(Value = "scilab")]
        Scilab,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Scss")]
        [Parameter(Value = "scss")]
        Scss,

        [Parameter(Value = "self")]
        Self,

        [Parameter(Value = "shell")]
        Shell,

        [Parameter(Value = "slash")]
        Slash,

        [Parameter(Value = "smalltalk")]
        Smalltalk,

        [Parameter(Value = "smarty")]
        Smarty,

        [Parameter(Value = "squirrel")]
        Squirrel,

        [Parameter(Value = "Standard ML")]
        StandardML,

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SuperCollider")]
        [Parameter(Value = "SuperCollider")]
        SuperCollider,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tcl")]
        [Parameter(Value = "tcl")]
        Tcl,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tcsh")]
        [Parameter(Value = "tcsh")]
        Tcsh,

        [Parameter(Value = "tea")]
        Tea,

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Te")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Te")]
        [Parameter(Value = "TeX")]
        TeX,

        [Parameter(Value = "textile")]
        Textile,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Toml")]
        [Parameter(Value = "toml")]
        Toml,

        [Parameter(Value = "turing")]
        Turing,

        [Parameter(Value = "twig")]
        Twig,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Txl")]
        [Parameter(Value = "txl")]
        Txl,

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TypeScript")]
        [Parameter(Value = "TypeScript")]
        TypeScript,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Parallel")]
        [Parameter(Value = "Unified Parallel C")]
        UnifiedParallelC,

        [Parameter(Value = "unknown")]
        Unknown,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vala")]
        [Parameter(Value = "vala")]
        Vala,

        [Parameter(Value = "verilog")]
        Verilog,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vhdl")]
        [Parameter(Value = "vhdl")]
        Vhdl,

        [Parameter(Value = "VimL")]
        VimL,

        [Parameter(Value = "visualbasic")]
        VisualBasic,

        [Parameter(Value = "volt")]
        Volt,

        [Parameter(Value = "wisp")]
        Wisp,

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Xc")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Xc")]
        [Parameter(Value = "xc")]
        Xc,

        [Parameter(Value = "xml")]
        Xml,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Proc")]
        [Parameter(Value = "XProc")]
        XProc,

        [Parameter(Value = "XQuery")]
        XQuery,

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Xs")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Xs")]
        [Parameter(Value = "xs")]
        Xs,

        [Parameter(Value = "xslt")]
        Xslt,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Xtend")]
        [Parameter(Value = "xtend")]
        Xtend,

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Yaml")]
        [Parameter(Value = "yaml")]
        Yaml
#pragma warning restore 1591
    }

    /// <summary>
    /// sorting repositories by any of below
    /// https://help.github.com/articles/searching-repositories#sorting
    /// </summary>
    public enum RepoSearchSort
    {
        /// <summary>
        /// search by number of stars
        /// </summary>
        [Parameter(Value = "stars")]
        Stars,
        /// <summary>
        /// search by number of forks
        /// </summary>
        [Parameter(Value = "forks")]
        Forks,
        /// <summary>
        /// search by last updated
        /// </summary>
        [Parameter(Value = "updated")]
        Updated
    }

    /// <summary>
    /// https://help.github.com/articles/searching-repositories#forks
    /// Specifying whether forked repositories should be included in results or not.
    /// </summary>
    public enum ForkQualifier
    {
        /// <summary>
        /// only search for forked repos
        /// </summary>
        [Parameter(Value = "Only")]
        OnlyForks,
        /// <summary>
        /// include forked repos into the search
        /// </summary>
        [Parameter(Value = "True")]
        IncludeForks
    }
}