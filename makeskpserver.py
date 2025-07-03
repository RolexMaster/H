import os
import xml.etree.ElementTree as ET
from flask import Flask, render_template

app = Flask(__name__)

def parse_xml_all(path):
    """폴더 내 모든 .xml 파일의 Tag/Attr 구조 동적 분석"""
    files = [f for f in os.listdir(path) if f.endswith('.xml')]
    all_rows = []
    all_columns = set()
    for fname in files:
        tree = ET.parse(os.path.join(path, fname))
        for elem in tree.iter():
            row = {"File": fname, "Tag": elem.tag}
            for k, v in elem.attrib.items():
                row[k] = v
                all_columns.add(k)
            all_rows.append(row)
            all_columns.update(['File', 'Tag'])  # 항상 포함
    # 컬럼 목록 정렬
    columns = [{'title': k, 'field': k, 'editor': "input"} for k in sorted(all_columns)]
    return all_rows, columns

@app.route("/")
def index():
    xml_dir = "./xmls"  # (여기에 XML 여러 개 넣으세요)
    data, columns = parse_xml_all(xml_dir)
    return render_template("table.html", data=data, columns=columns)

if __name__ == "__main__":
    app.run(debug=True)
