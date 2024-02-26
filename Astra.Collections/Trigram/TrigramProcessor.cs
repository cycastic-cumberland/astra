using System.Collections;

namespace Astra.Collections.Trigram;

public struct TrigramProcessorEnumerator<TUnit, TSequence>(TSequence key, TUnit defaultUnit) : IEnumerator<Trigram<TUnit>>
    where TUnit : IEquatable<TUnit>
    where TSequence : IReadOnlyList<TUnit>
{
    private readonly int _trimmedLength = key.Count - 2;
    private int _iterator = 0;
    private int _stage = -1;
    private TrigramBuilder<TUnit> _builder = TrigramBuilder<TUnit>.Create();
    private Trigram<TUnit> _current;

    public void Dispose()
    {
        
    }

    public bool MoveNext()
    {
        while (true)
        {
            switch (_stage)
            {
                case -1: // entering
                {
                    switch (key.Count)
                    {
                        case 0:
                        {
                            _stage = 0;
                            return false;
                        }
                        case 1:
                        {
                            _builder[0] = key[0];
                            _builder[1] = defaultUnit;
                            _builder[2] = defaultUnit;

                            _current = _builder.ToTrigram(1);
                            _stage = 1;
                            return true;
                        }
                        case 2:
                        {
                            _builder[0] = key[1];
                            _builder[1] = defaultUnit;
                            _builder[2] = defaultUnit;

                            _current = _builder.ToTrigram(1);
                            _stage = 3;

                            return true;
                        }
                        default:
                        {
                            _iterator = 0;
                            _stage = 6;
                            continue;
                        }
                    }
                }
                case 1: // key.Count == 1 stage 2
                {
                    _builder[0] = defaultUnit;
                    _builder[1] = key[0];
                    _builder[2] = defaultUnit;

                    _current = _builder.ToTrigram(1);
                    _stage++;
                    return true;
                }
                case 2: // key.Count == 1 stage 3
                {
                    _builder[0] = defaultUnit;
                    _builder[1] = defaultUnit;
                    _builder[2] = key[0];

                    _current = _builder.ToTrigram(1);
                    _stage = 0;
                    return true;
                }
                case 3: // key.Count == 1 stage 2
                {
                    _builder[0] = key[0];
                    _builder[1] = key[1];
                    _builder[2] = defaultUnit;

                    _current = _builder.ToTrigram(2);
                    _stage++;

                    return true;
                }
                case 4: // key.Count == 1 stage 3
                {
                    _builder[0] = defaultUnit;
                    _builder[1] = key[0];
                    _builder[2] = key[1];

                    _current = _builder.ToTrigram(2);
                    _stage++;

                    return true;
                }
                case 5: // key.Count == 1 stage 4
                {
                    _builder[0] = defaultUnit;
                    _builder[1] = defaultUnit;
                    _builder[2] = key[0];

                    _current = _builder.ToTrigram(1);
                    _stage = 0;

                    return true;
                }
                case 6: // default stage 2
                {
                    if (_iterator >= _trimmedLength)
                    {
                        _stage++;
                        continue;
                    }
                    
                    _builder[0] = key[_iterator];
                    _builder[1] = key[_iterator + 1];
                    _builder[2] = key[_iterator + 2];
                    _current = _builder.ToTrigram(3);
                    _iterator++;
                    
                    return true;
                }
                case 7: // default stage 3
                {
                    _builder[0] = defaultUnit;
                    _builder[1] = key[0];
                    _builder[2] = key[1];
                    _current = _builder.ToTrigram(2);
                    _stage++;
                    
                    return true;
                }
                case 8: // default stage 4
                {
                    _builder[0] = defaultUnit;
                    _builder[1] = defaultUnit;
                    _builder[2] = key[0];
                    _current = _builder.ToTrigram(1);
                    _stage++;
                    
                    return true;
                }
                case 9: // default stage 5
                {
                    _builder[0] = key[^2];
                    _builder[1] = key[^1];
                    _builder[2] = defaultUnit;
                    _current = _builder.ToTrigram(2);
                    _stage++;
                    
                    return true;
                }
                case 10: // default stage 6
                {
                    _builder[0] = key[^1];
                    _builder[1] = defaultUnit;
                    _builder[2] = defaultUnit;
                    _current = _builder.ToTrigram(1);
                    _stage = 0;
                    
                    return true;
                }
                default: // or 0, final stage
                    return false;
            }
        }
    }

    public void Reset()
    {
        _stage = -1;
    }

    public Trigram<TUnit> Current => _current;

    object IEnumerator.Current => Current;
}

public readonly struct TrigramProcessor<TUnit, TSequence>(TSequence key, TUnit defaultUnit) : IEnumerable<Trigram<TUnit>>
    where TUnit : IEquatable<TUnit>
    where TSequence : IReadOnlyList<TUnit>
{
    public TrigramProcessorEnumerator<TUnit, TSequence> GetEnumerator()
    {
        return new(key, defaultUnit);
    }
    
    IEnumerator<Trigram<TUnit>> IEnumerable<Trigram<TUnit>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public static class TrigramProcessor
{
    public static TrigramProcessor<TUnit, TSequence> ToTrigrams<TUnit, TSequence>(this TSequence key, TUnit defaultUnit)
        where TUnit : IEquatable<TUnit>
        where TSequence : IReadOnlyList<TUnit>
    {
        return new(key, defaultUnit);
    }
}