using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Astra.TypeErasure.Data;

namespace Astra.TypeErasure.QueryLanguage;

public partial class QueryLexer
{
    public struct Enumerator : IEnumerator<Token>
    {
        private readonly QueryLexer _host;
        private readonly IEnumerator _enumerator;
        private Token _token;
        
        internal Enumerator(QueryLexer host, string haystack)
        {
            _host = host;
            // ReSharper disable once GenericEnumeratorNotDisposed
            _enumerator = host._regex.Matches(haystack).GetEnumerator();
        }

        public void Dispose()
        {
            
        }

        private void ProcessNumber(Match match)
        {
            if (match.Groups[FractionGroup].Success)
            {
                _token = new()
                {
                    TokenType = Token.TokenTypes.Number,
                    Location = match.Index,
                    Storage = new DataCell(double.Parse(match.ValueSpan))
                };
                return;
            }
            _token = new()
            {
                TokenType = Token.TokenTypes.Number,
                Location = match.Index,
                Storage = new DataCell(long.Parse(match.ValueSpan))
            };
        }

        public bool MoveNext()
        {
            if (!_enumerator.MoveNext()) return false;
            var match = (Match)_enumerator.Current!;
            if (match.Groups[KeywordGroup].Success)
            {
                _token = new()
                {
                    TokenType = Token.TokenTypes.Keyword,
                    Location = match.Index,
                    Storage = new DataCell(match.ValueSpan)
                };
                return true;
            }

            if (match.Groups[NumberGroup].Success)
            {
                ProcessNumber(match);
                return true;
            }

            if (match.Groups[SymbolGroup].Success)
            {
                _token = new()
                {
                    TokenType = Token.TokenTypes.Symbol,
                    Location = match.Index,
                    Storage = new DataCell(match.ValueSpan)
                };
                return true;
            }
            
            if (match.Groups[OperatorGroup].Success)
            {
                throw new NotImplementedException();
            }
            
            if (match.Groups[StringGroup].Success)
            {
                _token = new()
                {
                    TokenType = Token.TokenTypes.String,
                    Location = match.Index,
                    Storage = new DataCell(match.ValueSpan[1..^1])
                };
                return true;
            }
            
            if (match.Groups[DelimiterGroup].Success)
            {
                throw new NotImplementedException();
            }

            throw new UnreachableException();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public Token Current => _token;

        object IEnumerator.Current => Current;
    }
}