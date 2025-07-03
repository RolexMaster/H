import os
import xml.etree.ElementTree as ET

def parse_xml_all(path):
    """path 폴더 내 모든 .xml 파일의 Tag/Attr 구조 동적 분석"""
    files = [f for f in os.listdir(path) if f.endswith('.xml')]
    all_rows = []
    all_columns = set()
    for fname in files:
        tree = ET.parse(os.path.join(path, fname))
        for elem in tree.iter():
            # 태그 + 모든 속성 조합
            row = {"File": fname, "Tag": elem.tag}
            for k, v in elem.attrib.items():
                row[k] = v
                all_columns.add(k)
            all_rows.append(row)
            all_columns.update(['File', 'Tag'])  # 항상 포함
    # 컬럼 목록 정렬
    columns = [{'title': k, 'field': k, 'editor': "input"} for k in sorted(all_columns)]
    return all_rows, columns
