using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class EquationHandler
{
    static readonly PList<string> nums = new PList<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    static readonly PList<UnitPrefix> prefixes = new PList<UnitPrefix> { new UnitPrefix("T", 12), new UnitPrefix("G", 9), new UnitPrefix("M", 6), new UnitPrefix("k", 3), new UnitPrefix("h", 2), new UnitPrefix("da", 1), new UnitPrefix("", 0), new UnitPrefix("d", -1),
        new UnitPrefix("c", -2), new UnitPrefix("m", -3), new UnitPrefix("μ", -6), new UnitPrefix("n", -9), new UnitPrefix("p", -12)};
    static readonly PList<string> units = new PList<string> { "g", "l", "m", "m3" };
    const string zeroDivRegex = @"^.*(?<=\/)0+\.?0*(?:[\^x\/\+\-].*)*$";
    const string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";

    public static int SigFigCount(string input)
    {
        input = RemoveCharacter(input, " ");
        input = RemoveCharacter(input, "-");
        input = RemoveCharacter(input, "+");
        PList<string> characters = SplitString(input);
        bool containsDecimal = characters.Contains(".");
        PList<int> removeIndexes = new PList<int>();
        for (int i = 0; i < characters.Count; i ++)
        {
            if (characters[i] == "0")
            {
                removeIndexes.Add(i);
            }
            else if (characters[i] != ".")
            {
                break;
            }
        }
        bool exponent = false;
        for (int i = 0; i < characters.Count; i++)
        {
            if (i + 3 < characters.Count && characters[i + 3] == "^")
            {
                exponent = true;
            }
            if (exponent)
            {
                removeIndexes.Add(i);
            }
        }
        for (int i = 0; i < removeIndexes.Count; i++)
        {
            characters.RemoveAt(removeIndexes[i] - i);
        }
        if (containsDecimal)
        {
            characters.Remove(".");
        }
        else
        {
            for (int i = characters.Count - 1; i >= 0; i--)
            {
                if (characters[i] == "0")
                {
                    characters.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
        }
        int output = characters.Count;
        return output;
    }

    public static int DecimalCount(string input) // returns the number of digits after the decimal
    {
        input = RemoveCharacter(input, " ");
        int decimalIndex = input.IndexOf(".");
        int output = 0;
        if (decimalIndex != -1)
        {
            output = input.Length - decimalIndex - 1;
        }
        return output;
    }

    public static int FindNumCount(string input) // returns the total number of digits
    {
        input = RemoveCharacter(input, " ");
        input = RemoveCharacter(input, "(");
        input = RemoveCharacter(input, ")");
        PList<string> parts = SplitOperation(input);
        PList<string> numParts = new PList<string>();
        bool numFound = false;
        foreach (string part in parts)
        {
            numFound = false;
            foreach (string num in nums)
            {
                if (part.Contains(num))
                {
                    numFound = true;
                }
            }
            if (numFound)
            {
                numParts.Add(part);
            }
        }
        int count = numParts.Count;
        return count;
    }

    public static string SolveEquation(string input, bool useSigfigs, int targetSigfig = 0)
    {
        Regex divRegex = new Regex(zeroDivRegex);
        input = RemoveCharacter(input, " ");
        input = input.Replace("*", "x");
        PList<string> characters = new PList<string>();
        characters.AddRange(SplitString(input));
        PList<string> resolvedCharas = new PList<string>();
        resolvedCharas.AddRange(ResolveParentheses(characters.ToPList()));
        string equation = "";
        foreach (string chara in resolvedCharas.ToPList())
        {
            equation += chara;
        }
        PList<Operation> operations = new PList<Operation>();
        operations.Add(new Operation(0, equation));
        string result = equation;
        int index = 0;
        Operation newOperation;
        PList<string> parts = SplitOperation(equation);
        PList<int> sigfigCounts = new PList<int>();
        PList<int> decimalCounts = new PList<int>();
        bool checkSigfig = false;
        foreach (string part in parts)
        {
            checkSigfig = false;
            if (!IsOperator(part) && part != "-" && part != "(" && part != ")")
            {
                if (index > 0)
                {
                    if (part != "10" && parts[index - 1] != "^")
                    {
                        checkSigfig = true;
                    }
                    else if (part == "10" && (index >= parts.Count - 1 || parts[index + 1] != "^"))
                    {
                        checkSigfig = true;
                    }
                }
                else
                {
                    checkSigfig = true;
                }
            }
            if (checkSigfig)
            {
                sigfigCounts.Add(SigFigCount(part));
                decimalCounts.Add(DecimalCount(part));
            }
            index++;
        }
        int lowestSigfig = 0;
        int lowestDecimal = 0;
        string startingOperation;
        index = 0;
        int removeIndex = 0;
        while (OperationCount(operations[0].operation) > 0)
        {
            characters.Clear();
            characters.AddRange(SplitString(operations[0].operation));
            resolvedCharas.Clear();
            resolvedCharas.AddRange(ResolveParentheses(characters.ToPList()));
            operations[0].operation = "";
            foreach (string chara in resolvedCharas.ToPList())
            {
                operations[0].operation += chara;
            }
            if (divRegex.IsMatch(operations[0].operation))
            {
                return "Undefined";
            }
            startingOperation = operations[0].operation;
            index = 0;
            while (OperationCount(operations[index].operation) > 1)
            {
                newOperation = FindNextOperation(operations[index].operation);
                removeIndex = operations[index].operation.IndexOf(newOperation.operation);
                operations[index].operation = operations[index].operation.Remove(removeIndex, newOperation.operation.Length);
                operations.Add(newOperation);
                index++;
            }
            if (OperationCount(startingOperation) > 1 || !useSigfigs)
            {
                result = SimpleOperation(operations[index].operation);
            }
            else
            {
                if (operations[index].operation.Contains("^"))
                {
                    result = SolveExponent(operations[index].operation);
                    break;
                }
                else if (operations[index].operation.Contains("x") || operations[index].operation.Contains("/"))
                {
                    lowestSigfig = targetSigfig;
                    if (targetSigfig == 0)
                    {
                        lowestSigfig = sigfigCounts[0];
                        foreach (int sigfig in sigfigCounts)
                        {
                            if (sigfig < lowestSigfig)
                            {
                                lowestSigfig = sigfig;
                            }
                        }
                    }
                    result = SigFigMultiply(operations[index].operation, lowestSigfig);
                    break;
                }
                else if (operations[index].operation.Contains("+") || operations[index].operation.Contains("-"))
                {
                    lowestDecimal = targetSigfig;
                    if (targetSigfig == 0)
                    {
                        lowestDecimal = decimalCounts[0];
                        foreach (int decNum in decimalCounts)
                        {
                            if (decNum < lowestDecimal)
                            {
                                lowestDecimal = decNum;
                            }
                        }
                    }
                    result = SigFigAddition(operations[index].operation, lowestDecimal);
                    break;
                }
            }
            operations[index].operation = result;
            if (operations.Count > 1)
            {
                for (int i = operations.Count - 1; i > 0; i--)
                {
                    newOperation = InsertOperation(operations[i], operations[i - 1]);
                    operations.RemoveAt(i);
                    operations[i - 1] = newOperation;
                    result = newOperation.operation;
                }
            }
            else
            {
                operations[0] = new Operation(0, result);
            }
        }
        string output = result;
        return output;
    }

    public static string SolveLog(string baseString, string logString)
    {
        baseString = SolveEquation(baseString, false);
        logString = SolveEquation(logString, false);
        double baseNum = Convert.ToDouble(baseString);
        double logNum = Convert.ToDouble(logString);
        double resultNum = Math.Log(logNum, baseNum);
        string result = FixExponent(resultNum.ToString("r"));
        string output = result;
        return output;
    }

    public static string SolveExponent(string input)
    {
        PList<string> characters = SplitString(input);
        string baseString = "";
        string expoString = "";
        bool foundExpo = false;
        foreach (string chara in characters)
        {
            if (chara == "^")
            {
                foundExpo = true;
            }
            else
            {
                if (!foundExpo)
                {
                    baseString += chara;
                }
                else
                {
                    expoString += chara;
                }
            }
        }
        double baseNum = Convert.ToDouble(baseString);
        double expoNum = Convert.ToDouble(expoString);
        double result = Math.Pow(baseNum, expoNum);
        string output = FixExponent(result.ToString("r"));
        return output;
    }

    public static string SimpleOperation(string input)
    {
        input = RemoveCharacter(input, " ");
        input = RemoveCharacter(input, "(");
        input = RemoveCharacter(input, ")");
        PList<string> parts = SplitOperation(input);
        double result = Convert.ToDouble(parts[0]);
        int lastIndex = parts.Count - 1;
        if (parts.Contains("^"))
        {
            result = Convert.ToDouble(SolveExponent(input));
        }
        else if (parts.Contains("x"))
        {
            result *= Convert.ToDouble(parts[lastIndex]);
        }
        else if (parts.Contains("/"))
        {
            result /= Convert.ToDouble(parts[lastIndex]);
        }
        else
        {
            result += Convert.ToDouble(parts[lastIndex]);
        }
        string output = FixExponent(result.ToString("r"));
        return output;
    }

    public static int OperationCount(string input)
    {
        input = RemoveCharacter(input, " ");
        PList<string> characters = SplitString(input);
        characters = ResolveParentheses(characters);
        PList<string> operators = new PList<string>();
        int index = 0;
        foreach (string chara in characters)
        {
            if (index > 0)
            {
                if (IsOperator(chara))
                {
                    operators.Add(chara);
                }
                else if (chara == "-" && !IsOperator(characters[index - 1]))
                {
                    operators.Add(chara);
                }
            }
            index++;
        }
        int count = operators.Count;
        return count;
    }

    public static bool IsOperator(string input)
    {
        bool output = false;
        if (input == "^" || input == "x" || input == "/" || input == "+")
        {
            output = true;
        }
        return output;
    }

    public static Operation InsertOperation(Operation toInsert, Operation parent)
    {
        PList<string> parentChars = SplitString(parent.operation);
        PList<string> insertChars = SplitString(toInsert.operation);
        PList<string> resultChars = new PList<string>();
        resultChars.AddRange(parentChars);
        resultChars.InsertRange(toInsert.index, insertChars);
        string result = "";
        foreach (string chara in resultChars)
        {
            result += chara;
        }
        Operation output = new Operation(parent.index, result);
        return output;
    }

    public static PList<string> ResolveParentheses(PList<string> input)
    {
        PList<string> output = new PList<string>();
        PList<int> opensIndexes = new PList<int>();
        PList<int> closesIndexes = new PList<int>();
        string part = "";
        int index = 0;
        foreach (string chara in input.ToPList())
        {
            if (chara == "(")
            {
                opensIndexes.Add(index);
            }
            else if (chara == ")")
            {
                closesIndexes.Add(index);
            }
            index++;
        }
        int diff = opensIndexes.Count - closesIndexes.Count;
        if (diff > 0)
        {
            for (int i = 0; i < diff; i++)
            {
                input.Add(")");
            }
        }
        else if (diff < 0)
        {
            for (int i = 0; i < MathF.Abs(diff); i++)
            {
                input.Insert(0, "(");
            }
        }
        index = 0;
        foreach (string chara in input.ToPList())
        {
            if (chara == "(")
            {
                if (index > 0 && (nums.Contains(input[index - 1]) || input[index - 1] == ")"))
                {
                    output.Add("x");
                }
                output.Add(chara);
            }
            else if (chara == ")")
            {
                output.Add(chara);
                if (index < input.Count - 1 && nums.Contains(input[index + 1]))
                {
                    output.Add("x");
                }
            }
            else
            {
                output.Add(chara);
            }
            index++;
        }
        input = output.ToPList();
        output.Clear();
        opensIndexes.Clear();
        closesIndexes.Clear();
        index = 0;
        foreach (string chara in input.ToPList())
        {
            if (chara == "(")
            {
                opensIndexes.Add(index);
            }
            else if (chara == ")")
            {
                part = "";
                for (int i = opensIndexes[opensIndexes.Count - 1]; i <= index; i++)
                {
                    part += input[i];
                }
                if (FindNumCount(part) == 1)
                {
                    input.RemoveAt(index);
                    for (int i = 0; i < opensIndexes.Count; i++)
                    {
                        if (opensIndexes[i] > index)
                        {
                            opensIndexes[i]--;
                        }
                    }
                    index -= 2;
                    input.RemoveAt(opensIndexes[opensIndexes.Count - 1]);
                }
                opensIndexes.RemoveAt(opensIndexes.Count - 1);
            }
            index++;
        }
        foreach (string chara in input)
        {
            output.Add(chara);
        }
        string test = "";
        foreach (string chara in output)
        {
            test += chara;
        }
        return output.ToPList();
    }

    public static Operation FindNextOperation(string input)
    {
        input = RemoveCharacter(input, " ");
        PList<string> characters = SplitString(input);
        PList<string> charasToFind = new PList<string>();
        if (characters.Contains("("))
        {
            charasToFind.Add("(");
        }
        else if (characters.Contains("^"))
        {
            charasToFind.Add("^");
        }
        else if (characters.Contains("x") || characters.Contains("/"))
        {
            charasToFind.Add("x");
            charasToFind.Add("/");
        }
        else if (characters.Contains("+") || characters.Contains("-"))
        {
            charasToFind.Add("+");
            charasToFind.Add("-");
        }
        int operationIndex = 0;
        int index = 0;
        foreach (string chara in characters)
        {
            if (charasToFind.Contains(chara))
            {
                if (chara != "(")
                {
                    int i = index - 1;
                    while (i >= 0)
                    {
                        if (i == 0 || !nums.Contains(characters[i - 1]) && characters[i - 1] != ".")
                        {
                            break;
                        }
                        i--;
                    }
                    operationIndex = i;
                }
                else
                {
                    operationIndex = index + 1;
                }
                break;
            }
            index++;
        }
        string operation = "";
        bool secondNum = false;
        bool firstNeg = false;
        int openCount = 0;
        for (int i = operationIndex; i < characters.Count; i++)
        {
            if (!charasToFind.Contains("("))
            {
                if (!nums.Contains(characters[i]) && characters[i] != "." && !firstNeg && characters[i] != "-")
                {
                    if (!secondNum)
                    {
                        secondNum = true;
                    }
                    else
                    {
                        break;
                    }
                }
                if (characters[i] == "-")
                {
                    firstNeg = true;
                }
            }
            else
            {
                if (characters[i] == "(")
                {
                    openCount++;
                }
                else if (characters[i] == ")")
                {
                    openCount--;
                }
                if (openCount == -1)
                {
                    break;
                }
            }
            operation += characters[i];
        }
        Operation output = new Operation(operationIndex, operation);
        return output;
    }

    public static string SigFigAddition(string input, int decimalCount = -1)
    {
        input = RemoveCharacter(input, " ");
        PList<string> parts = SplitOperation(input);
        PList<int> decimalNums = new PList<int>();
        double result = 0;
        foreach (string part in parts.ToPList())
        {
            double number;
            if (part != "" && part != null)
            {
                if (Double.TryParse(part, out number))
                {
                    number = Convert.ToDouble(part);
                    decimalNums.Add(DecimalCount(part));
                    result += number;
                }
            }
            else
            {
                parts.Remove(part);
            }
        }
        int lowestDecimal = decimalCount;
        if (decimalCount == -1)
        {
            lowestDecimal = decimalNums[0];
            foreach (int decNum in decimalNums)
            {
                if (decNum < lowestDecimal)
                {
                    lowestDecimal = decNum;
                }
            }
        }
        string rounding = "F" + lowestDecimal;
        string output = FixExponent(result.ToString(rounding));
        return output;
    }

    public static string SigFigMultiply(string input, int sigfigCount = 0) // can only accept equations with a single operator, works for multiplication and division
    {
        input = RemoveCharacter(input, " ");
        PList<string> parts = SplitOperation(input);
        PList<int> sigfigNums = new PList<int>();
        bool num = false;
        double result = 0;
        string operation = "";
        foreach (string part in parts)
        {
            num = false;
            foreach (string number in nums)
            {
                if (part.Contains(number))
                {
                    num = true;
                    break;
                }
            }
            if (num)
            {
                sigfigNums.Add(SigFigCount(part));
                double number = Convert.ToDouble(part);
                if (operation == "x")
                {
                    result *= number;
                }
                else if (operation == "/")
                {
                    result /= number;
                }
                else
                {
                    result += number;
                }
            }
            else
            {
                operation = part;
            }
        }
        int lowestSigfig = sigfigCount;
        if (sigfigCount == 0)
        {
            lowestSigfig = sigfigNums[0];
            for (int i = 1; i < sigfigNums.Count; i++)
            {
                if (sigfigNums[i] < lowestSigfig)
                {
                    lowestSigfig = sigfigNums[i];
                }
            }
        }
        string finalNum = FixExponent(result.ToString("r"));
        int currentSigfig = SigFigCount(finalNum);
        int diff = lowestSigfig - currentSigfig;
        bool decimalTried = false;
        Regex zeroRegex = new Regex(zeroRegexString);
        while (currentSigfig != lowestSigfig && !zeroRegex.IsMatch(finalNum))
        {
            finalNum = SetSigFigs(finalNum, lowestSigfig, decimalTried);
            currentSigfig = SigFigCount(finalNum);
            diff = lowestSigfig - currentSigfig;
            if (diff < 0)
            {
                decimalTried = true;
            }
        }
        string output = finalNum;
        return output;
    }

    public static string SetSigFigs(string input, int numSigfig, bool decimalTried)
    {
        input = RemoveCharacter(input, " ");
        int currentSigfig = SigFigCount(input);
        int diff = currentSigfig - numSigfig;
        string result = input;
        if (diff < 0 && !decimalTried)
        {
            if (result.Contains("."))
            {
                for (int i = 0; i < -diff; i++)
                {
                    result += "0";
                }
            }
            else
            {
                result += ".";
                if (result.ElementAt(result.Length - 2) != '0')
                {
                    result += "0";
                }
            }
        }
        else if (decimalTried)
        {
            result = SigFigSciNota(result, numSigfig);
        }
        else if (diff > 0)
        {
            PList<string> characters = SplitString(result);
            int index = -1;
            int sigfigsFound = 0;
            bool intFound = false;
            while (sigfigsFound < numSigfig)
            {
                index++;
                if (index < characters.Count)
                {
                    if (nums.Contains(characters[index]))
                    {
                        if (!intFound)
                        {
                            if (characters[index] != "0")
                            {
                                intFound = true;
                                sigfigsFound++;
                            }
                        }
                        else
                        {
                            sigfigsFound++;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            bool decimalFound = false;
            int decimalIndex = -1;
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] == ".")
                {
                    decimalFound = true;
                    decimalIndex = i;
                    break;
                }
            }
            if (decimalFound && decimalIndex < characters.Count - 1)
            {
                if (index < decimalIndex - 1)
                {
                    PList<int> removeIndexes = new PList<int>();
                    for (int i = decimalIndex; i < characters.Count; i++)
                    {
                        removeIndexes.Add(i);
                    }
                    for (int i = 0; i < removeIndexes.Count; i++)
                    {
                        characters.RemoveAt(removeIndexes[i] - i);
                    }
                    result = "";
                    foreach (string chara in characters)
                    {
                        result += chara;
                    }
                    result = SigFigUp(result, numSigfig);
                }
                else
                {
                    if (index < decimalIndex)
                    {
                        index++;
                    }
                    int roundingInt = index - decimalIndex;
                    string rounding = "F" + roundingInt;
                    result = "";
                    foreach (string chara in characters)
                    {
                        result += chara;
                    }
                    double num = Convert.ToDouble(result);
                    result = FixExponent(num.ToString(rounding));
                }
            }
            else if (decimalFound)
            {
                characters.Remove(".");
                result = "";
                foreach (string chara in characters)
                {
                    result += chara;
                }
                result = SigFigUp(result, numSigfig);
            }
            else
            {
                result = "";
                foreach (string chara in characters)
                {
                    result += chara;
                }
                result = SigFigUp(result, numSigfig);
            }
        }
        string output = result;
        return output;
    }

    public static string SigFigUp(string input, int numSigfig)
    {
        input = RemoveCharacter(input, " ");
        double num = Convert.ToDouble(input);
        string result = input;
        PList<string> characters = SplitString(result);
        int numBeforeDecimal = 0;
        if (characters.Contains("."))
        {
            numBeforeDecimal = characters.IndexOf(".");
        }
        else
        {
            numBeforeDecimal = characters.Count;
        }
        if (num < 0)
        {
            numBeforeDecimal -= 1;
        }
        int index = 0;
        while (numBeforeDecimal > numSigfig)
        {
            num /= 10;
            index++;
            result = FixExponent(num.ToString("r"));
            characters = SplitString(result);
            if (characters.Contains("."))
            {
                numBeforeDecimal = characters.IndexOf(".");
            }
            else
            {
                numBeforeDecimal = characters.Count;
            }
            if (num < 0)
            {
                numBeforeDecimal -= 1;
            }
        }
        num = Math.Round(num, MidpointRounding.AwayFromZero);
        num *= Math.Pow(10, index);
        result = FixExponent(num.ToString("r"));
        string output = result;
        return output;
    }

    public static string SigFigSciNota(string input, int numSigfig)
    {
        input = RemoveCharacter(input, " ");
        if (input.Contains("x"))
        {
            return null;
        }
        double num = Convert.ToDouble(input);
        int exponent = 0;
        int index = 0;
        while (Math.Abs(num) >= 10)
        {
            num /= 10;
            index++;
        }
        while (Math.Abs(num) < 1)
        {
            num *= 10;
            index--;
        }
        exponent = index;
        string numString = FixExponent(num.ToString("r"));
        PList<string> charas = SplitString(numString);
        int numBeforeDecimal = 0;
        bool hasDecimal = charas.Contains(".");
        string result = "";
        if (hasDecimal)
        {
            foreach (string chara in charas)
            {
                if (chara != ".")
                {
                    numBeforeDecimal++;
                }
                else
                {
                    break;
                }
            }
            int diff = numSigfig - numBeforeDecimal;
            string rounding = "F" + diff;
            result = FixExponent(num.ToString(rounding));
        }
        else
        {
            result = FixExponent(num.ToString("F0"));
        }
        if (!result.Contains(".") && SigFigCount(result) < numSigfig)
        {
            result += ".";
        }
        while (SigFigCount(result) < numSigfig)
        {
            result += "0";
        }
        if (SigFigCount(result) > numSigfig)
        {
            PList<string> characters = SplitString(result);
            while (SigFigCount(result) > numSigfig)
            {
                characters.RemoveAt(characters.Count - 1);
                result = "";
                foreach (string chara in characters)
                {
                    result += chara;
                }
            }
        }
        result += "x" + 10 + "^" + exponent;
        string output = result;
        return output;
    }

    public static string Sci2Normal(string input)
    {
        input = RemoveCharacter(input, " ");
        PList<string> characters = SplitString(input);
        string numString = "";
        string exponentString = "";
        bool xFound = false;
        bool expoFound = false;
        foreach (string chara in characters)
        {
            if (expoFound)
            {
                exponentString += chara;
            }
            else
            {
                if (chara == "^")
                {
                    expoFound = true;
                }
                else if (chara == "x")
                {
                    xFound = true;
                }
                else if (!xFound)
                {
                    numString += chara;
                }
            }
        }
        double num = Convert.ToDouble(numString);
        double exponent = Convert.ToDouble(exponentString);
        double multiple = Math.Pow(10, exponent);
        num *= multiple;
        string output = FixExponent(num.ToString("r"));
        return output;
    }

    public static string ConvertUnits(string input, int curPrefI, int curUnitI, int endPrefI, int endUnitI, int expo)
    {
        string startUnit = units[curUnitI];
        string endUnit = units[endUnitI];
        string curUnit = startUnit;
        int curExpo = expo;
        int blankPrefI = 6;
        int deciPrefI = blankPrefI + 1;
        bool cubed = false;
        if (startUnit == "l" && endUnit == "m3")
        {
            curExpo = ConvertUnitPrefix(curPrefI, blankPrefI, curExpo, false);
            curExpo = ConvertUnitPrefix(deciPrefI, endPrefI, curExpo, true);
        }
        else if (startUnit == "m3" && endUnit == "l")
        {
            curExpo = ConvertUnitPrefix(curPrefI, deciPrefI, curExpo, true);
            curExpo = ConvertUnitPrefix(blankPrefI, endPrefI, curExpo, false);
        }
        else
        {
            if (startUnit == "m3")
            {
                cubed = true;
            }
            curExpo = ConvertUnitPrefix(curPrefI, endPrefI, curExpo, cubed);
        }
        input = SigFigSciNota(input, SigFigCount(input));
        PList<string> charas = SplitString(input);
        bool foundX = false;
        bool foundExpo = false;
        string extraExpo = "";
        string baseNum = "";
        foreach (string chara in charas)
        {
            if (foundExpo)
            {
                extraExpo += chara;
            }
            else if (chara == "^")
            {
                foundExpo = true;
            }
            else if (chara == "x")
            {
                foundX = true;
            }
            else if (!foundX)
            {
                baseNum += chara;
            }
        }
        curExpo += Convert.ToInt32(extraExpo);
        input = baseNum + "x10^" + curExpo;
        string output = input + " " + prefixes[endPrefI].prefix + endUnit;
        return output;
    }

    public static int ConvertUnitPrefix(int currentPrefixIndex, int targetPrefixIndex, int exponent, bool cubed)
    {
        int startMult = prefixes[currentPrefixIndex].multiple;
        int endMult = prefixes[targetPrefixIndex].multiple;
        int diff = startMult - endMult;
        if (cubed)
        {
            diff *= 3;
        }
        int output = exponent + diff;
        return output;
    }

    public static PList<string> SplitString(string input)
    {
        char[] chars = input.ToCharArray();
        PList<string> characters = new PList<string>();
        foreach (char chara in chars)
        {
            characters.Add(chara.ToString());
        }
        return characters;
    }

    public static PList<string> SplitOperation(string input)
    {
        input = RemoveCharacter(input, " ");
        PList<string> output = new PList<string>();
        PList<string> characters = SplitString(input);
        PList<PList<string>> parts = new PList<PList<string>>();
        int partNum = -1;
        bool changed = true;
        bool num = false;
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] == ".")
            {
                if (!num)
                {
                    changed = true;
                }
                num = true;
            }
            else if (characters[i] == "+" || characters[i] == "-")
            {
                changed = true;
                num = true;
            }
            else
            {
                if (nums.Contains(characters[i]))
                {
                    if (!num)
                    {
                        changed = true;
                    }
                    num = true;
                }
                else
                {
                    num = false;
                }
            }
            if (!num)
            {
                changed = true;
            }
            if (changed)
            {
                parts.Add(new PList<string>());
                partNum++;
                changed = false;
            }
            parts[partNum].Add(characters[i]);
        }
        for (int i = 0; i < parts.Count; i++)
        {
            output.Add("");
            foreach (string chara in parts[i])
            {
                output[i] += chara;
            }
        }
        return output;
    }

    public static string FixExponent(string input)
    {
        string result = input;
        bool negative = false;
        if (input.Contains('E'))
        {
            result = "";
            PList<string> charas = SplitString(input);
            bool foundExpo = false;
            string expoString = "";
            string num = "";
            foreach (string chara in charas)
            {
                if (foundExpo)
                {
                    expoString += chara;
                }
                else if (chara == "E")
                {
                    foundExpo = true;
                }
                else
                {
                    num += chara;
                }
            }
            int expoNum = Convert.ToInt32(expoString);
            int decimalIndex = num.Length;
            if (num.Contains('.'))
            {
                decimalIndex = num.IndexOf('.');
            }
            if (expoNum > 0)
            {
                int targetDecimalIndex = decimalIndex + expoNum;
                num = RemoveCharacter(num, ".");
                if (num.Length < targetDecimalIndex)
                {
                    while (num.Length < targetDecimalIndex)
                    {
                        num += "0";
                    }
                }
                else if (num.Length > targetDecimalIndex)
                {
                    num = num.Insert(targetDecimalIndex, ".");
                }
                result = num;
            }
            else if (expoNum < 0)
            {
                negative = true;
                int numAfterDecimal = DigitsAfterDecimal(num);
                int targetAfterDecimal = Mathf.Abs(expoNum) + numAfterDecimal;
                while (numAfterDecimal < targetAfterDecimal)
                {
                    if (decimalIndex > 0)
                    {
                        decimalIndex--;
                    }
                    else
                    {
                        num = num.Insert(1, "0");
                    }
                    num = RemoveCharacter(num, ".");
                    num = num.Insert(decimalIndex, ".");
                    numAfterDecimal = DigitsAfterDecimal(num);
                }
                result = num;
            }
            else
            {
                result = num;
            }
        }
        if (result.ElementAt(0) == '.')
        {
            result = result.Insert(0, "0");
        }
        PList<string> characters = SplitString(result);
        if (negative)
        {
            for (int i = characters.Count - 1; i >= 0; i--)
            {
                if (characters[i] == "0" || characters[i] == ".")
                {
                    characters.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
        }
        result = "";
        foreach (string chara in characters)
        {
            result += chara;
        }
        string output = result;
        return output;
    }

    public static int DigitsAfterDecimal(string input)
    {
        PList<string> charas = SplitString(input);
        int output = 0;
        bool foundDecimal = false;
        foreach (string chara in charas)
        {
            if (foundDecimal)
            {
                output++;
            }
            else if (chara == ".")
            {
                foundDecimal = true;
            }
        }
        return output;
    }

    public static string RoundToDecimalPoint(string input, int decimalCount)
    {
        string output = input;
        if (DigitsAfterDecimal(input) > decimalCount)
        {
            double num = Convert.ToDouble(input);
            string rounding = $"F{decimalCount}";
            string result = num.ToString(rounding);
            PList<string> charas = SplitString(result);
            int start = charas.Count;
            for (int i = start - 1; i >= 0; i--)
            {
                if (charas[i] == "0")
                {
                    charas.RemoveAt(i);
                }
                else
                {
                    if (charas[i] == ".")
                    {
                        charas.RemoveAt(i);
                    }
                    break;
                }
            }
            result = "";
            foreach (string chara in charas)
            {
                result += chara;
            }
            output = result;
        }
        return output;
    }

    public static string RemoveCharacter(string input, string character)
    {
        string output = input.Replace(character, string.Empty);
        return output;
    }

    public static UnitPrefix GetPrefix(int index)
    {
        return prefixes[index];
    }

    public static string GetUnit(int index)
    {
        return units[index];
    }
}

public class Operation
{
    public int index;
    public string operation;

    public Operation(int index, string operation)
    {
        this.index = index;
        this.operation = operation;
    }
}

public class UnitPrefix
{
    public string prefix;
    public int multiple;

    public UnitPrefix(string prefix, int multiple)
    {
        this.prefix = prefix;
        this.multiple = multiple;
    }
}
