using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class GrammarProcessor
{
    static Dictionary<string, List<List<string>>> productions = new Dictionary<string, List<List<string>>>();
    static Dictionary<string, HashSet<string>> firstSets = new Dictionary<string, HashSet<string>>();
    static Dictionary<string, HashSet<string>> followSets = new Dictionary<string, HashSet<string>>();
    static HashSet<string> nonTerminals = new HashSet<string>();
    static HashSet<string> terminals = new HashSet<string>();

    static void Main()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("📘 Grammar Rule Input");
        Console.ResetColor();


        Console.Write("Enter Grammer Rules OR type end to exit....\n ");
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            
            Console.Write("Enter rule: ");
            Console.ResetColor();

            string line = Console.ReadLine();
            if (line.Trim().ToLower() == "end") break;

            var parts = Regex.Split(line.Trim(), @"\s*->\s*|\s*→\s*");
            if (parts.Length != 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Invalid rule format. Use: NonTerminal → production1 | production2");
                Console.ResetColor();
                return;
            }

            string lhs = parts[0].Trim();
            nonTerminals.Add(lhs);

            var rhsOptions = parts[1]
                .Split('|')
                .Select(opt => opt.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList())
                .ToList();

            foreach (var option in rhsOptions)
            {
                if (option.Count > 0 && option[0] == lhs)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Grammar invalid for top-down parsing (Left recursion detected).");
                    Console.ResetColor();
                    return;
                }
            }

            if (!productions.ContainsKey(lhs))
                productions[lhs] = new List<List<string>>();

            foreach (var option in rhsOptions)
            {
                productions[lhs].Add(option);
            }
        }

        // Identify terminals
        foreach (var rule in productions)
        {
            foreach (var option in rule.Value)
            {
                foreach (var symbol in option)
                {
                    if (!nonTerminals.Contains(symbol) && symbol != "ε")
                        terminals.Add(symbol);
                }
            }
        }

        string startSymbol = productions.Keys.First();

        // Compute FIRST sets
        foreach (var nt in nonTerminals)
        {
            ComputeFirst(nt);
        }

        // Compute FOLLOW sets
        ComputeFollow(startSymbol);

        // Pretty Print Table
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n📊 FIRST & FOLLOW TABLE:");
        Console.WriteLine("┌───────────────┬──────────────────────────┬────────────────────┐");
        Console.WriteLine("│ Non-Terminal  │ FIRST Set                │ FOLLOW Set         │");
        Console.WriteLine("├───────────────┼──────────────────────────┼────────────────────┤");

        foreach (var nt in nonTerminals)
        {
            string first = string.Join(", ", firstSets[nt]);
            string follow = string.Join(", ", followSets[nt]);

            Console.WriteLine($"│ {nt,-13} │ {{ {first,-22} }} │ {{ {follow,-16} }} │");
        }

        Console.WriteLine("└───────────────┴──────────────────────────┴────────────────────┘");
        Console.ResetColor();
    }

    static void ComputeFirst(string symbol)
    {
        if (firstSets.ContainsKey(symbol)) return;
        firstSets[symbol] = new HashSet<string>();

        if (terminals.Contains(symbol) || symbol == "ε")
        {
            firstSets[symbol].Add(symbol);
            return;
        }

        foreach (var production in productions[symbol])
        {
            for (int i = 0; i < production.Count; i++)
            {
                string sym = production[i];
                ComputeFirst(sym);

                foreach (var f in firstSets[sym])
                {
                    if (f != "ε")
                        firstSets[symbol].Add(f);
                }

                if (!firstSets[sym].Contains("ε"))
                    break;

                if (i == production.Count - 1)
                    firstSets[symbol].Add("ε");
            }
        }
    }

    static void ComputeFollow(string startSymbol)
    {
        foreach (var nt in nonTerminals)
            followSets[nt] = new HashSet<string>();

        followSets[startSymbol].Add("$");

        bool changed = true;

        while (changed)
        {
            changed = false;

            foreach (var lhs in productions.Keys)
            {
                foreach (var production in productions[lhs])
                {
                    for (int i = 0; i < production.Count; i++)
                    {
                        string B = production[i];
                        if (!nonTerminals.Contains(B)) continue;

                        int initialCount = followSets[B].Count;

                        if (i + 1 < production.Count)
                        {
                            string next = production[i + 1];
                            ComputeFirst(next);

                            foreach (var f in firstSets[next])
                            {
                                if (f != "ε")
                                    followSets[B].Add(f);
                            }

                            if (firstSets[next].Contains("ε"))
                            {
                                foreach (var f in followSets[lhs])
                                    followSets[B].Add(f);
                            }
                        }
                        else
                        {
                            foreach (var f in followSets[lhs])
                                followSets[B].Add(f);
                        }

                        if (followSets[B].Count > initialCount)
                            changed = true;
                    }
                }
            }
        }
    }
}
