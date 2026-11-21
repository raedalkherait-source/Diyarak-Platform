import type { ListingStatistics } from "@/lib/statistics";
import { niceMax } from "@/lib/statistics";

const dayFormat = new Intl.DateTimeFormat("ar-SY-u-nu-latn", { day: "numeric", month: "short", timeZone: "UTC" });
const numberFormat = new Intl.NumberFormat("ar-SY-u-nu-latn");

/** Publisher statistics: three totals and a daily-views bar chart with a table fallback. */
export function ListingStatsPanel({ stats }: { stats: ListingStatistics }) {
  const width = 600;
  const height = 160;
  const top = 8;
  const bottom = 22;
  const plot = height - top - bottom;
  const max = niceMax(stats.days.map((day) => day.views));
  const slot = width / stats.days.length;
  const barWidth = Math.max(2, slot - 2); // 2px surface gap between bars
  const label = (iso: string) => dayFormat.format(new Date(`${iso}T00:00:00Z`));

  return (
    <section className="panel stats-panel" aria-labelledby="stats-title">
      <h2 id="stats-title">إحصاءات آخر {numberFormat.format(stats.days.length)} يوماً</h2>
      <div className="stat-tiles">
        <div className="stat-tile"><span>المشاهدات</span><strong>{numberFormat.format(stats.views)}</strong></div>
        <div className="stat-tile"><span>إظهار رقم التواصل</span><strong>{numberFormat.format(stats.contactReveals)}</strong></div>
        <div className="stat-tile"><span>في المفضلة الآن</span><strong>{numberFormat.format(stats.favorites)}</strong></div>
      </div>

      <figure className="stats-chart">
        <figcaption>المشاهدات اليومية</figcaption>
        <svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label={`المشاهدات اليومية، المجموع ${stats.views}`} preserveAspectRatio="none">
          {[0, 0.5, 1].map((fraction) => (
            <line key={fraction} className="grid" x1={0} x2={width} y1={top + plot * (1 - fraction)} y2={top + plot * (1 - fraction)} />
          ))}
          {stats.days.map((day, index) => {
            const barHeight = (day.views / max) * plot;
            // Right-to-left: the oldest day sits on the right, today on the left.
            const x = width - (index + 1) * slot + 1;
            return (
              <g key={day.day} className="bar">
                <rect className="hit" x={x - 1} y={top} width={slot} height={plot} />
                {day.views > 0 ? (
                  <path className="value" d={topRoundedBar(x, top + plot, barWidth, barHeight)} />
                ) : null}
                <title>{`${label(day.day)}: ${day.views} مشاهدة، ${day.contactReveals} إظهار رقم`}</title>
              </g>
            );
          })}
        </svg>
        <div className="stats-axis" aria-hidden="true">
          {/* Right-to-left flex: the first label sits on the right, under the oldest day. */}
          <span>{label(stats.days[0]?.day ?? stats.from)}</span>
          <span>أعلى قيمة: {numberFormat.format(max)}</span>
          <span>{label(stats.days[stats.days.length - 1]?.day ?? stats.to)}</span>
        </div>
      </figure>

      <details>
        <summary>عرض الأرقام كجدول</summary>
        <table className="stats-table">
          <thead>
            <tr><th scope="col">اليوم</th><th scope="col">المشاهدات</th><th scope="col">إظهار الرقم</th></tr>
          </thead>
          <tbody>
            {[...stats.days].reverse().map((day) => (
              <tr key={day.day}><td>{label(day.day)}</td><td>{day.views}</td><td>{day.contactReveals}</td></tr>
            ))}
          </tbody>
        </table>
      </details>
      <p className="notice">لا تُحتسب زياراتك لإعلانك ولا زيارات محركات البحث.</p>
    </section>
  );
}

/** A bar anchored to the baseline with only its data end (the top) rounded. */
function topRoundedBar(x: number, baseline: number, width: number, height: number): string {
  const r = Math.min(4, width / 2, height);
  const top = baseline - height;
  return [
    `M${x},${baseline}`,
    `V${top + r}`,
    `Q${x},${top} ${x + r},${top}`,
    `H${x + width - r}`,
    `Q${x + width},${top} ${x + width},${top + r}`,
    `V${baseline}`,
    "Z",
  ].join(" ");
}
