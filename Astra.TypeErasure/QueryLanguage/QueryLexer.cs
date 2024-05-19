using System.Text.RegularExpressions;

namespace Astra.TypeErasure.QueryLanguage;

public partial class QueryLexer
{
    internal const string KeywordGroup = "keyword";
    
    internal const string NumberGroup = "number";
    internal const string SignGroup = "sign";
    internal const string IntegerGroup = "integer";
    internal const string FractionGroup = "fraction";
    
    internal const string SymbolGroup = "symbol";
    internal const string OperatorGroup = "operator";
    internal const string StringGroup = "string";
    internal const string DelimiterGroup = "delimeter";

    internal const string Expression =
        @$"(?<{KeywordGroup}>(?i)insert|(?i)select)|" + 
        @$"(?<{NumberGroup}>(?<{SignGroup}>\+|-)?(?<{IntegerGroup}>[0-9]+)(?<{FractionGroup}>\.[0-9]+)?)|((\+|-)?\.?[0-9]+)|" +
        @$"(?<{SymbolGroup}>[\w\d]+)|" +
        $@"(?<{OperatorGroup}>=|==|<|>|<=|>=|!=|\(|\)|(?i)and|(?i)or)|" +
        $@"(?<{StringGroup}>'[^']*')|" +
        $@"(?<{DelimiterGroup}>;|,)";
    [GeneratedRegex(Expression, RegexOptions.None, "en-US")]
    private static partial Regex CreateQueryRegex();

    private readonly Regex _regex;
    private readonly string[] _groupNames;

    private ReadOnlySpan<string> GroupNames => _groupNames.AsSpan()[1..];
    
    public QueryLexer()
    {
        _regex = CreateQueryRegex();
        _groupNames = _regex.GetGroupNames();
    }
}