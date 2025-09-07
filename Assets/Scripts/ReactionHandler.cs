using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using UnityEngine;

public class ReactionHandler
{
    const string compDivRegexString = @"^(?<comp>\(?(?:[A-Z][a-z]?[0-9]*)+(?:\)[0-9]*)?)+$";
    const string compDivReactRegexString = @"^(?<comp>\(?(?:[0-9]*[A-Z][a-z]?[0-9]*)+(?:\)[0-9]*)?)+$";
    const string compExpRegexString = @"^\(?(?<comp>(?:[A-Z][a-z]?[0-9]*)+)\)?(?<count>[0-9]*)?$";
    const string compExpReactRegexString = @"^\(?(?<count>[0-9]*)?(?<comp>(?:[A-Z][a-z]?[0-9]*)+)\)?$";
    const string elemPullRegexString = @"^(?<elem>[A-Z][a-z]?[0-9]*)+$";
    const string elemSplitRegexString = @"^(?<elem>[A-Z][a-z]?)(?<count>[1-9]*)$";
    const string compCountRegexString = @"^(?<count>[0-9]*)(?<formula>(?:\(?[A-Z][a-z]?[\)0-9]*)+)$";
    static ElementContainerSO elemCont;

    public static PList<string> BalanceReaction(PList<string> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            input[i] = EquationHandler.RemoveCharacter(input[i], " ");
        }
        if (!ValidateReaction(input))
        {
            return null;
        }
        List<string> output = new PList<string>();
        BalanceData compounds = new BalanceData(SplitReactionComps(input[0]), SplitReactionComps(input[1]));
        int index = 0;
        while (!CheckBalance(compounds))
        {
            compounds = NextBalanceStep(compounds);
            if (index > 99)
            {
                return null;
            }
            index++;
        }
        output.Add(CompoundsToString(compounds.reactants));
        output.Add(CompoundsToString(compounds.products));
        return output.ToPList();
    }

    public static BalanceData NextBalanceStep(BalanceData compounds)
    {
        PList<Element> reactElems = SplitCompounds(compounds.reactants);
        reactElems = CondenseElements(reactElems);
        PList<Element> prodElems = SplitCompounds(compounds.products);
        prodElems = CondenseElements(prodElems);
        PList<UnbalancedElem> unbElems = FindUnbalancedElems(reactElems, prodElems);
        UnbalancedElem elemToBalance = FindElemToBalance(compounds, unbElems);
        BalanceData output = BalanceElement(compounds, elemToBalance);
        return output;
    }

    public static BalanceData BalanceElement(BalanceData reaction, UnbalancedElem element)
    {
        PList<Compound> compounds = new PList<Compound>();
        if (element.reactantLower)
        {
            compounds.AddRange(reaction.reactants);
        }
        else
        {
            compounds.AddRange(reaction.products);
        }
        compounds = IncreaseCompound(compounds, element.symbol);
        BalanceData output = new BalanceData(reaction.reactants, reaction.products);
        if (element.reactantLower)
        {
            output.reactants = compounds.ToPList();
        }
        else
        {
            output.products = compounds.ToPList();
        }
        return output;
    }

    public static PList<Compound> IncreaseCompound(PList<Compound> compInput, string elemSymbol)
    {
        PList<Compound> output = new PList<Compound>();
        PList<Compound> compounds = compInput.ToPList();
        Compound compToIncrease = compounds[0];
        if (compounds.Count > 1)
        {
            PList<Compound> compsWithElem = FindCompsWithElem(compounds, elemSymbol);
            compToIncrease = compsWithElem[0];
            foreach (Compound comp in compsWithElem)
            {
                if (comp.elems.Count == 1)
                {
                    compToIncrease = comp;
                    break;
                }
            }
        }
        int index = compounds.IndexOf(compToIncrease);
        compToIncrease.count++;
        compounds[index] = compToIncrease;
        output = compounds;
        return output.ToPList();
    }

    public static PList<Compound> FindCompsWithElem(PList<Compound> compounds, string elemSymbol)
    {
        PList<Compound> output = new PList<Compound>();
        foreach (Compound comp in compounds.ToList())
        {
            foreach (Element elem in comp.elems)
            {
                if (elem.symbol == elemSymbol)
                {
                    output.Add(comp);
                    break;
                }
            }
        }
        return output.ToPList();
    }

    public static UnbalancedElem FindElemToBalance(BalanceData compounds, PList<UnbalancedElem> unbElems)
    {
        UnbalancedElem output = unbElems[0];
        if (unbElems.Count > 0)
        {
            bool foundSingle = false;
            foreach (UnbalancedElem elem in unbElems)
            {
                if (FindElemCountOnSide(elem.symbol, compounds, elem.reactantLower) == 1)
                {
                    foundSingle = true;
                    output = elem;
                    break;
                }
            }
            if (!foundSingle)
            {
                output = FindSoloElem(unbElems, compounds);
            }
        }
        return output;
    }

    public static PList<UnbalancedElem> FindUnbalancedElems(PList<Element> reactElemsInput, PList<Element> prodElemsInput)
    {
        PList<Element> reactElems = reactElemsInput.ToPList();
        reactElems.Sort();
        PList<Element> prodElems = prodElemsInput.ToPList();
        prodElems.Sort();
        PList<UnbalancedElem> output = new PList<UnbalancedElem>();
        for (int i = 0; i < reactElems.Count; i++)
        {
            if (!reactElems[i].FullEquals(prodElems[i]))
            {
                bool reactantLower = reactElems[i].count < prodElems[i].count;
                output.Add(new UnbalancedElem(reactElems[i].symbol, reactantLower));
            }
        }
        return output.ToPList();
    }

    public static UnbalancedElem FindSoloElem(PList<UnbalancedElem> elems, BalanceData reaction)
    {
        PList<Compound> compounds = new PList<Compound>();
        UnbalancedElem output = null;
        foreach (UnbalancedElem elem in elems.ToPList())
        {
            if (elem.reactantLower)
            {
                compounds = reaction.reactants.ToPList();
            }
            else
            {
                compounds = reaction.products.ToPList();
            }
            foreach (Compound comp in compounds)
            {
                if (comp.elems.Count == 1 && comp.elems[0].symbol == elem.symbol)
                {
                    output = elem;
                    return output;
                }
            }
        }
        return output;
    }

    public static int FindElemCountOnSide(string elemSymbol, BalanceData reaction, bool reactant)
    {
        PList<Compound> compounds = new PList<Compound>();
        if (reactant)
        {
            compounds.AddRange(reaction.reactants);
        }
        else
        {
            compounds.AddRange(reaction.products);
        }
        int count = 0;
        foreach (Compound comp in compounds)
        {
            foreach (Element elem in comp.elems)
            {
                if (elem.symbol == elemSymbol)
                {
                    count++;
                }
            }
        }
        int output = count;
        return output;
    }

    public static bool CheckBalance(BalanceData reaction)
    {
        PList<Element> reactElems = SplitCompounds(reaction.reactants);
        reactElems = CondenseElements(reactElems);
        reactElems.Sort(); // sort by atomic symbol in alphabetical order
        PList<Element> prodElems = SplitCompounds(reaction.products);
        prodElems = CondenseElements(prodElems);
        prodElems.Sort();
        return reactElems.SequenceFullEqual(prodElems);
    }

    public static PList<Element> SplitCompounds(PList<Compound> input)
    {
        PList<Element> output = new PList<Element>();
        foreach (Compound comp in input.ToPList())
        {
            PList<Compound> comps = new PList<Compound>() { comp };
            string compString = CompoundsToString(comps);
            output.AddRange(SplitCompoundString(compString, true));
        }
        return output.ToPList();
    }

    public static PList<Element> ElementCount(PList<Compound> input)
    {
        PList<Element> output = new PList<Element>();
        PList<Element> elems = new PList<Element>();
        foreach (Compound comp in input.ToList())
        {
            elems.AddRange(comp.elems);
        }
        output = CondenseElements(elems);
        return output.ToPList();
    }

    public static PList<Compound> SplitReactionComps(string input)
    {
        PList<Compound> output = new PList<Compound>();
        PList<string> compStrings = SplitAtPlus(input);
        foreach (string compString in compStrings)
        {
            output.Add(StringToCompound(compString));
        }
        return output.ToPList();
    }

    public static string CompoundsToString(PList<Compound> compounds)
    {
        string output = "";
        for (int i = 0; i < compounds.Count; i++)
        {
            if (i > 0)
            {
                output += "+";
            }
            string count = "";
            if (compounds[i].count > 1)
            {
                count = compounds[i].count.ToString();
            }
            output += count;
            foreach (Element elem in compounds[i].elems)
            {
                count = "";
                if (elem.count > 1)
                {
                    count = elem.count.ToString();
                }
                output += elem.symbol + count;
            }
        }
        return output;
    }

    public static Compound StringToCompound(string input)
    {
        Regex regex = new Regex(compCountRegexString);
        Match match = regex.Match(input);
        string formula = match.Groups["formula"].Value;
        string countString = match.Groups["count"].Value;
        int count = 1;
        if (countString != string.Empty)
        {
            count = Convert.ToInt32(countString);
        }
        List<Element> elems = SplitCompoundString(formula);
        Compound output = new Compound(elems.ToPList(), count);
        return output;
    }

    public static PList<string> SplitAtPlus(string input)
    {
        PList<string> output = new PList<string>();
        if (input.Contains('+'))
        {
            PList<string> charas = EquationHandler.SplitString(input);
            string part = "";
            foreach (string chara in charas)
            {
                if (chara == "+")
                {
                    output.Add(part);
                    part = "";
                }
                else
                {
                    part += chara;
                }
            }
            output.Add(part);
        }
        else
        {
            output.Add(input);
        }
        return output.ToPList();
    }

    public static bool ValidateReaction(PList<string> input)
    {
        BalanceData reaction = new BalanceData(SplitReactionComps(input[0]), SplitReactionComps(input[1]));
        PList<Element> reactElems = ElementCount(reaction.reactants);
        foreach (Element elem in reactElems)
        {
            if (!ValidateElement(elem))
            {
                return false;
            }
        }
        reactElems.Sort();
        PList<Element> prodElems = ElementCount(reaction.products);
        foreach (Element elem in prodElems)
        {
            if (!ValidateElement(elem))
            {
                return false;
            }
        }
        prodElems.Sort();
        bool output = reactElems.SequenceEqual(prodElems);
        return output;
    }

    public static bool ValidateElement(Element input)
    {
        bool output = true;
        if (elemCont.GetElement(input.symbol, ElemSearchMode.atomicSymbol) == null)
        {
            output = false;
        }
        return output;
    }

    public static bool ValidateCompound(string input)
    {
        PList<Element> elements = SplitCompoundString(input);
        bool output = true;
        foreach (Element elem in elements)
        {
            if (elemCont.GetElement(elem.symbol, ElemSearchMode.atomicSymbol) == null)
            {
                output = false;
                break;
            }
        }
        return output;
    }

    public static string CalculateCompoundMass(string input)
    {
        PList<Element> elements = SplitCompoundString(input);
        string equation = "";
        string mass;
        string result = "";
        PList<int> decimalCounts = new PList<int>();
        if (elements.Count > 1)
        {
            foreach (Element elem in elements)
            {
                mass = elemCont.GetElement(elem.symbol, ElemSearchMode.atomicSymbol).mass;
                decimalCounts.Add(EquationHandler.DecimalCount(mass));
                if (equation != "")
                {
                    equation += "+";
                }
                equation += $"(({mass})x{elem.count})";
            }
            int rounding = decimalCounts[0];
            foreach (int decimalCount in decimalCounts)
            {
                if (decimalCount > rounding)
                {
                    rounding = decimalCount;
                }
            }
            result = EquationHandler.SolveEquation(equation, true, rounding);
        }
        else
        {
            mass = elemCont.GetElement(elements[0].symbol, ElemSearchMode.atomicSymbol).mass;
            equation = $"({mass})x{elements[0].count}";
            result = EquationHandler.SolveEquation(equation, false);
        }
        string output = result;
        return output;
    }

    public static PList<Element> SplitCompoundString(string input, bool react = false, bool condense = true)
    {
        PList<Element> output = new PList<Element>();
        PList<string> dividedCompound = DivideCompoundAtParentheses(input, react);
        string expandedCompound = ExpandCompound(dividedCompound.ToPList(), react);
        if (condense)
        {
            PList<Element> elems = StringToElemList(expandedCompound);
            output.AddRange(CondenseElements(elems));
        }
        else
        {
            output.AddRange(StringToElemList(input));
        }
        return output.ToPList();
    }

    public static PList<Element> CondenseElements(PList<Element> input) // combine all elements with the same symbol into one Element class with the total count
    {
        PList<Element> output = new PList<Element>();
        int symbolsChecked = 0;
        while (symbolsChecked < input.Count)
        {
            Element condElem = input[0];
            input.RemoveAt(0);
            foreach (Element elem in input.ToPList())
            {
                if (condElem.Equals(elem))
                {
                    condElem.count += elem.count;
                    input.Remove(elem);
                }
            }
            input.Add(condElem);
            symbolsChecked++;
        }
        output.AddRange(input);
        return output.ToPList();
    }

    public static PList<Element> StringToElemList(string input)
    {
        PList<Element> output = new PList<Element>();
        Regex elemPullRegex = new Regex(elemPullRegexString);
        Regex elemSplitRegex = new Regex(elemSplitRegexString);
        Match match = elemPullRegex.Match(input);
        CaptureCollection elems = match.Groups["elem"].Captures;
        foreach (Capture elem in elems)
        {
            match = elemSplitRegex.Match(elem.Value);
            string symbol = match.Groups["elem"].Value;
            string countString = match.Groups["count"].Value;
            int count = 1;
            if (countString != string.Empty)
            {
                count = Convert.ToInt32(countString);
            }
            output.Add(new Element(symbol, count));
        }
        return output.ToPList();
    }

    public static string ExpandCompound(PList<string> input, bool react)
    {
        string output = "";
        string regexString = compExpRegexString;
        if (react)
        {
            regexString = compExpReactRegexString;
        }
        Regex compExpRegex = new Regex(regexString);
        foreach (string section in input)
        {
            Match match = compExpRegex.Match(section);
            string comp = match.Groups["comp"].Captures[0].Value;
            string countString = match.Groups["count"].Captures[0].Value;
            int count = 1;
            if (countString != string.Empty)
            {
                count = Convert.ToInt32(countString);
            }
            for (int i = 0; i < count; i++)
            {
                output += comp;
            }
        }
        return output;
    }

    public static PList<string> DivideCompoundAtParentheses(string input, bool react)
    {
        PList<string> output = new PList<string>();
        string regexString = compDivRegexString;
        if (react)
        {
            regexString = compDivReactRegexString;
        }
        Regex compDivRegex = new Regex(regexString);
        Match match = compDivRegex.Match(input);
        CaptureCollection comps = match.Groups["comp"].Captures;
        foreach (Capture comp in comps)
        {
            output.Add(comp.Value);
        }
        return output.ToPList();
    }

    public static void SetElemCont(ElementContainerSO newElemCont)
    {
        elemCont = newElemCont;
    }
}

public class Compound : IEquatable<Compound>
{
    public PList<Element> elems = new PList<Element>();
    public int count;

    public bool FullEquals(Compound compareComp)
    {
        if (compareComp == null) return false;
        else return elems.SequenceEqual(compareComp.elems) && count == compareComp.count;
    }

    public bool Equals(Compound compareComp)
    {
        if (compareComp == null) return false;
        else return elems.SequenceEqual(compareComp.elems) && count == compareComp.count;
    }

    public override bool Equals(object obj) => Equals(obj as Compound);

    public override int GetHashCode() => (elems, count).GetHashCode();

    public override string ToString() => $"{count}{elems}";

    public Compound(PList<Element> elems, int count)
    {
        this.elems = elems.ToPList();
        this.count = count;
    }
}

public class Element : IComparable<Element>, IEquatable<Element>
{
    public string symbol;
    public int count;

    public bool FullEquals(Element compareElem)
    {
        return symbol == compareElem.symbol && count == compareElem.count;
    }

    public bool Equals(Element compareElem)
    {
        return symbol == compareElem.symbol;
    }

    public int CompareTo(Element compareElem)
    {
        if (compareElem == null) return 1;
        else return symbol.CompareTo(compareElem.symbol);
    }

    public override bool Equals(object obj) => Equals(obj as Element);

    public override int GetHashCode() => symbol.GetHashCode();

    public int GetFullHashCode() => (symbol, count).GetHashCode();

    public override string ToString() => $"{symbol}{count}";

    public Element(string symbol, int count)
    {
        this.symbol = symbol;
        this.count = count;
    }
}

public class ElemFullEqualComparer : IEqualityComparer<Element>
{
    public bool Equals(Element x, Element y)
    {
        if (x == null || y == null) return false;
        else return x.symbol == y.symbol && x.count == y.count;
    }

    public int GetHashCode(Element elem) => elem.GetFullHashCode();
}

public class BalanceData
{
    public PList<Compound> reactants = new PList<Compound>();
    public PList<Compound> products = new PList<Compound>();

    public override string ToString() => $"{reactants}->{products}";

    public BalanceData(PList<Compound> reactants, PList<Compound> products)
    {
        this.reactants = reactants;
        this.products = products;
    }
}

public class UnbalancedElem
{
    public string symbol;
    public bool reactantLower;

    public override string ToString() => $"{symbol}, {reactantLower}";

    public UnbalancedElem(string symbol, bool reactantLower)
    {
        this.symbol = symbol;
        this.reactantLower = reactantLower;
    }
}

public class PList<T> : List<T>
{
    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        foreach (var item in this)
        {
            if (result.Length > 0)
            {
                result.Append(", " + item.ToString());
            }
            else
            {
                result.Append(item.ToString());
            }
        }
        return $"[{result}]";
    }

    public PList() : base() { }

    public PList(IEnumerable<T> collection) : base(collection) { }
}

public static class CustomExtensions
{
    public static PList<T> ToPList<T>(this IEnumerable<T> source)
    {
        using var transaction = new TransactionScope();
        PList<T> result = new PList<T>(source);
        transaction.Complete();
        return result;
    }

    public static bool SequenceFullEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer = null)
    {
        comparer ??= (IEqualityComparer<TSource>)new ElemFullEqualComparer();
        return Enumerable.SequenceEqual(first, second, comparer);
    }
}
