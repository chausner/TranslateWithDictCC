import argparse
import json
from collections import defaultdict


def split_description(description: str) -> Optional[Tuple[str, str]]:
    SPECIAL_CASES = {
        "Medicină Dentară / Stomatologie / Zahnmedizin": ("Medicină Dentară / Stomatologie", "Zahnmedizin"),
        "Medicină Dentară / Stomatologie / Dental Medicine": ("Medicină Dentară / Stomatologie", "Dental Medicine")
    }
    if description in SPECIAL_CASES:
        return SPECIAL_CASES[description]

    descriptions = description.split(" / ")
    if len(descriptions) != 2:
        print(f"Error: description {description!r} has unexpected format")
        return None
    return descriptions[0], descriptions[1]


def build_deen_reverse_map(subjects) -> Dict[str, Dict[str, str]]: # language -> description -> canonical subject
    deen_reverse_map: Dict[str, Dict[str, str]] = defaultdict(dict) 

    for subject, description in subjects["DEEN"].items():
        def add_reverse_map_entry(language: str, description: str):
            if description not in deen_reverse_map[language]:
                deen_reverse_map[language][description] = subject
            elif deen_reverse_map[language][description] != subject:
                print(f"Warning: conflicting subjects for {language} {description}: {deen_reverse_map[language][description]!r} vs {subject!r}")

        descriptions = split_description(description)
        if descriptions is None:
            continue
        add_reverse_map_entry("DE", descriptions[1])
        add_reverse_map_entry("EN", descriptions[0])

    return deen_reverse_map


def get_canonical_subject(
    deen_reverse_map: Dict[str, Dict[str, str]],
    language1: str,
    description1: str,
    language2: str,
    description2: str
) -> Optional[str]:
    if language1 in ("DE", "EN"):
        if description1 in deen_reverse_map[language1]:
            return deen_reverse_map[language1][description1]          
        else:
            print(f"Error: could not map description {description1!r} in language {language1} to a canonical subject")
            return None
    elif language2 in ("DE", "EN"):
        if description2 in deen_reverse_map[language2]:
            return deen_reverse_map[language2][description2]          
        else:
            print(f"Error: could not map description {description2!r} in language {language2} to a canonical subject")
            return None
    else:
        print(f"Error: unexpected language pair {language1!r}, {language2!r}")
        return None


def main(args: argparse.Namespace):
    with open(args.input, "r", encoding="utf-8") as f:
        subjects = json.load(f)

    # add missing subjects that are not in the input data, but are needed to link the languages together
    subjects["DEEN"]["TM"] = "Trademarks, Brand Names / Marken, Warenzeichen"
    subjects["DEEN"]["pej."] = "Offensive or Pejorative Terms, Expletives / Schimpfwörter und Kraftausdrücke"

    deen_reverse_map = build_deen_reverse_map(subjects)

    results: Dict[str, Dict[str, Tuple[str, str]]] = defaultdict(dict) # canonical subject -> language -> (subject, description)

    for language_pair in subjects.keys():
        language1 = language_pair[:2]
        language2 = language_pair[2:]
        for subject, description in subjects[language_pair].items():
            descriptions = split_description(description)
            if descriptions is None:
                continue
            if language1 > language2: # order of descriptions depends on order of languages
                description1, description2 = descriptions[0], descriptions[1]
            else:
                description1, description2 = descriptions[1], descriptions[0]

            canonical_subject = get_canonical_subject(deen_reverse_map, language1, description1, language2, description2)
            if canonical_subject is None:
                continue

            def add_results_entry(language: str, description: str):
                if language not in results[canonical_subject]:
                    results[canonical_subject][language] = (subject, description)
                elif results[canonical_subject][language] != (subject, description):
                    print(f"Warning: conflicting subjects for {language} {description}: {results[canonical_subject][language]!r} vs {(subject, description)!r}")

            if language2 in ("DE", "EN"):
                add_results_entry(language1, description1)             
            if language1 in ("DE", "EN"):
                add_results_entry(language2, description2)

    with open(args.output, "w", encoding="utf-8") as f:
        results_json = [
            { language: {
                "subject": subject_description_tuple[0],
                "description": subject_description_tuple[1] } for (language, subject_description_tuple) in item.items() }
            for item in results.values()
        ]
        json.dump(results_json, f, indent=2, ensure_ascii=False)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Transform Subjects.json into a language-centric subject list.")
    parser.add_argument(
        "--input",
        required=True,
        help="Path to the input Subjects.json file.",
    )
    parser.add_argument(
        "--output",
        required=True,
        help="Path to the output JSON file.",
    )
    args = parser.parse_args()
    main(args)