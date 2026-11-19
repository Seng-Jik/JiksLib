#!/usr/bin/env python3
"""校验 Cobertura 覆盖率报告的 line-rate 是否达到阈值。

用法：check_coverage.py <results-dir> <min-line-rate-percent>
- 在 <results-dir> 下递归查找 coverage.cobertura.xml；
- 找不到报告直接失败（避免采集器失效时静默通过）；
- 同一目录下有多份报告时取最差值，避免被较高的一份掩盖。
"""

import glob
import sys
import xml.etree.ElementTree as ET


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: check_coverage.py <results-dir> <min-line-rate-percent>")
        return 2

    results_dir = sys.argv[1]
    try:
        threshold = float(sys.argv[2])
    except ValueError:
        print(f"::error::invalid threshold: {sys.argv[2]}")
        return 2

    reports = sorted(glob.glob(f"{results_dir}/**/coverage.cobertura.xml", recursive=True))

    if not reports:
        print(f"::error::no coverage report found under {results_dir}")
        return 2

    rates = []

    for path in reports:
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError as error:
            print(f"::error::cannot parse {path}: {error}")
            return 2

        rate = float(root.get("line-rate", "0"))
        rates.append(rate)
        print(f"{path}: line-rate={rate:.4f}")

    worst = min(rates)
    print(f"line coverage: {worst:.2%} (threshold {threshold:.2f}%)")

    if worst * 100 < threshold:
        print("::error::coverage is below the required threshold")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
