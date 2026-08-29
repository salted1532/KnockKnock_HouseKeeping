using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Tools > Dialogue > Import CSV → DialogueDatabase
// Assets/My/Data/Dialogue/ 아래 모든 *.csv 를 읽어 단일 DialogueDatabase.asset 을 통째로 재생성한다.
// 열: npcId,situation,day,role,nodeKey,label,expression,text,goto,outcome
//   (첫 줄이 "npcId" 로 시작하면 헤더로 건너뜀. goto/outcome 은 없어도 됨)
// 같은 (npcId,situation,day,role,nodeKey) 연속 행 = 한 노드.
//   text 채워진 행 = 대사 줄. text 비고 label/goto 있는 행 = 선택지.
//   text 안의 \n 은 실제 줄바꿈으로 변환.
public static class DialogueImporter
{
    private const string CsvFolder = "Assets/My/Data/Dialogue";
    private const string DbPath = "Assets/My/Data/Dialogue/DialogueDatabase.asset";

    [MenuItem("Tools/Dialogue/Import CSV → DialogueDatabase")]
    public static void Import()
    {
        if (!Directory.Exists(CsvFolder))
        {
            Debug.LogError($"[DialogueImporter] 폴더 없음: {CsvFolder}");
            return;
        }

        var db = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(DbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<DialogueDatabase>();
            AssetDatabase.CreateAsset(db, DbPath);
        }
        db.entries.Clear();

        var knownIds = new HashSet<int>(
            AssetDatabase.FindAssets("t:NpcData")
                .Select(g => AssetDatabase.LoadAssetAtPath<NpcData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(n => n != null)
                .Select(n => n.id));
        var unknownIds = new HashSet<int>();

        var files = Directory.GetFiles(CsvFolder, "*.csv", SearchOption.AllDirectories);
        int rowCount = 0;
        DialogueEntry current = null;
        string currentSig = null;

        foreach (var file in files)
        {
            var rawLines = File.ReadAllLines(file);
            for (int i = 0; i < rawLines.Length; i++)
            {
                string raw = rawLines[i];
                if (i == 0 && raw.TrimStart().StartsWith("npcId")) continue;   // 헤더
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var cols = ParseCsvLine(raw);
                if (cols.Count < 8)
                {
                    Debug.LogWarning($"[DialogueImporter] {Path.GetFileName(file)}:{i + 1} 열 부족({cols.Count}/8+) → 스킵");
                    continue;
                }

                if (!int.TryParse(cols[0].Trim(), out int npcId))
                {
                    Debug.LogWarning($"[DialogueImporter] {Path.GetFileName(file)}:{i + 1} npcId '{cols[0]}' 숫자 아님 → 스킵");
                    continue;
                }
                var situation = ParseEnum(cols[1].Trim(), Situation.Reception);
                int.TryParse(cols[2].Trim(), out int day);
                var role = ParseEnum(cols[3].Trim(), EntryRole.Greeting);
                string nodeKey = cols[4].Trim();
                string label = cols[5].Trim();
                var expr = ParseEnum(cols[6].Trim(), Expression.Neutral);
                string text = cols[7].Replace("\\n", "\n");
                string goTo = cols.Count > 8 ? cols[8].Trim() : "";
                string outcomeStr = cols.Count > 9 ? cols[9].Trim() : "";

                if (!knownIds.Contains(npcId)) unknownIds.Add(npcId);

                string sig = $"{npcId}|{situation}|{day}|{role}|{nodeKey}";
                if (sig != currentSig)
                {
                    current = new DialogueEntry
                    {
                        npcId = npcId, situation = situation, day = day,
                        role = role, nodeKey = nodeKey,
                    };
                    db.entries.Add(current);
                    currentSig = sig;
                }

                bool hasText = !string.IsNullOrEmpty(text);
                if (hasText)
                {
                    current.lines.Add(new DialogueLine { expression = expr, text = text });
                    if (!string.IsNullOrEmpty(label)) current.label = label;
                    if (!string.IsNullOrEmpty(goTo)) current.goToNode = goTo;
                    if (!string.IsNullOrEmpty(outcomeStr))
                        current.outcome = ParseEnum(outcomeStr, Verdict.None);
                }
                else if (!string.IsNullOrEmpty(label) || !string.IsNullOrEmpty(goTo))
                {
                    current.choices.Add(new Choice { label = label, goToNode = goTo });
                }
                else
                {
                    Debug.LogWarning($"[DialogueImporter] {Path.GetFileName(file)}:{i + 1} text/label/goto 전부 빔 → 스킵");
                    continue;
                }
                rowCount++;
            }
        }

        db.RebuildIndex();
        ValidateGotos(db);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DialogueImporter] CSV {files.Length}개 / {rowCount}행 → 노드 {db.entries.Count}개. ({DbPath})");
        if (unknownIds.Count > 0)
            Debug.LogWarning($"[DialogueImporter] NpcData 없는 npcId: {string.Join(", ", unknownIds.OrderBy(x => x))}");
    }

    // 모든 goToNode / choice.goToNode 가 실제 노드를 가리키는지 검사 (빈 값은 '허브 복귀'로 허용).
    private static void ValidateGotos(DialogueDatabase db)
    {
        var keys = new Dictionary<(int, Situation), HashSet<string>>();
        foreach (var e in db.entries)
        {
            if (string.IsNullOrEmpty(e.nodeKey)) continue;
            var k = (e.npcId, e.situation);
            if (!keys.TryGetValue(k, out var set)) keys[k] = set = new HashSet<string>();
            set.Add(e.nodeKey);
        }

        int dangling = 0;
        void Check(int npc, Situation sit, string target, string where)
        {
            if (string.IsNullOrEmpty(target)) return;
            if (!keys.TryGetValue((npc, sit), out var set) || !set.Contains(target))
            {
                Debug.LogWarning($"[DialogueImporter] 끊긴 goto: npc {npc} {sit} — '{target}' ({where})");
                dangling++;
            }
        }

        foreach (var e in db.entries)
        {
            Check(e.npcId, e.situation, e.goToNode, $"노드 '{e.nodeKey}'");
            foreach (var c in e.choices)
                Check(e.npcId, e.situation, c.goToNode, $"'{e.nodeKey}' 선택지 '{c.label}'");
        }
        if (dangling == 0) Debug.Log("[DialogueImporter] goto 검사 통과");
    }

    // 따옴표/이스케이프("") 지원하는 간단 CSV 한 줄 파서.
    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(ch);
            }
            else
            {
                if (ch == '"') inQuotes = true;
                else if (ch == ',') { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(ch);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    private static T ParseEnum<T>(string s, T fallback) where T : struct =>
        System.Enum.TryParse<T>(s, true, out var v) ? v : fallback;

    // 조회·분기 규칙 자가 점검. (프레임워크 없이 assert)
    [MenuItem("Tools/Dialogue/Self-Check Query")]
    public static void SelfCheck()
    {
        var db = ScriptableObject.CreateInstance<DialogueDatabase>();
        db.entries.Add(new DialogueEntry { npcId = 1, situation = Situation.Reception, day = 0, role = EntryRole.Greeting, lines = { new DialogueLine { text = "공통" } } });
        db.entries.Add(new DialogueEntry { npcId = 1, situation = Situation.Reception, day = 3, role = EntryRole.Greeting, lines = { new DialogueLine { text = "3일차" } } });
        db.entries.Add(new DialogueEntry { npcId = 1, situation = Situation.Reception, day = 0, role = EntryRole.Node, nodeKey = "leave", lines = { new DialogueLine { text = "나가세요" } }, outcome = Verdict.Rejected });
        db.RebuildIndex();

        Debug.Assert(db.Query(1, Situation.Reception, 3, EntryRole.Greeting)[0].lines[0].text == "3일차", "3일차 전용이 우선");
        Debug.Assert(db.Query(1, Situation.Reception, 1, EntryRole.Greeting)[0].lines[0].text == "공통", "전용 없으면 day 0 폴백");
        Debug.Assert(db.Query(1, Situation.Dawn, 1, EntryRole.Greeting).Count == 0, "상황 불일치 → 빈 결과");
        Debug.Assert(db.Query(2, Situation.Reception, 1, EntryRole.Greeting).Count == 0, "미등록 npcId → 빈 결과");
        Debug.Assert(db.GetNode(1, Situation.Reception, "leave", 1)?.outcome == Verdict.Rejected, "GetNode 로 노드 조회 + outcome");
        Debug.Assert(db.GetNode(1, Situation.Reception, "nope", 1) == null, "없는 nodeKey → null");
        Object.DestroyImmediate(db);
        Debug.Log("[DialogueImporter] Self-Check 통과");
    }
}
