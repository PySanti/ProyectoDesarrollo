import { SVGProps } from "react";

export type IconProps = SVGProps<SVGSVGElement>;

function svgProps(props: IconProps): IconProps {
  return {
    width: 24,
    height: 24,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 2,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    "aria-hidden": true,
    focusable: false,
    ...props
  };
}

export type IconComponent = (props: IconProps) => JSX.Element;

export const BrandMark: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M5 21V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16" />
    <line x1="3" y1="21" x2="21" y2="21" />
    <line x1="14.5" y1="12" x2="14.5" y2="13.5" />
  </svg>
);

export const Users: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
    <circle cx="9" cy="7" r="4" />
    <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
    <path d="M16 3.13a4 4 0 0 1 0 7.75" />
  </svg>
);

export const UserPlus: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
    <circle cx="9" cy="7" r="4" />
    <line x1="19" y1="8" x2="19" y2="14" />
    <line x1="22" y1="11" x2="16" y2="11" />
  </svg>
);

export const ListChecks: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M3 17l2 2 4-4" />
    <path d="M3 7l2 2 4-4" />
    <line x1="13" y1="6" x2="21" y2="6" />
    <line x1="13" y1="12" x2="21" y2="12" />
    <line x1="13" y1="18" x2="21" y2="18" />
  </svg>
);

export const Plus: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <line x1="12" y1="5" x2="12" y2="19" />
    <line x1="5" y1="12" x2="19" y2="12" />
  </svg>
);

export const Play: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <polygon points="6 3 20 12 6 21 6 3" />
  </svg>
);

export const MapPin: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z" />
    <circle cx="12" cy="10" r="3" />
  </svg>
);

export const Flag: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z" />
    <line x1="4" y1="22" x2="4" y2="15" />
  </svg>
);

export const LogOut: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
    <polyline points="16 17 21 12 16 7" />
    <line x1="21" y1="12" x2="9" y2="12" />
  </svg>
);

export const PanelLeft: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <rect x="3" y="3" width="18" height="18" rx="2" />
    <line x1="9" y1="3" x2="9" y2="21" />
  </svg>
);

export const Menu: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <line x1="4" y1="6" x2="20" y2="6" />
    <line x1="4" y1="12" x2="20" y2="12" />
    <line x1="4" y1="18" x2="20" y2="18" />
  </svg>
);

export const X: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <line x1="18" y1="6" x2="6" y2="18" />
    <line x1="6" y1="6" x2="18" y2="18" />
  </svg>
);

export const AlertTriangle: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
    <line x1="12" y1="9" x2="12" y2="13" />
    <line x1="12" y1="17" x2="12.01" y2="17" />
  </svg>
);

export const Lock: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <rect x="3" y="11" width="18" height="11" rx="2" />
    <path d="M7 11V7a5 5 0 0 1 10 0v4" />
  </svg>
);

export const Compass: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <circle cx="12" cy="12" r="10" />
    <polygon points="16.24 7.76 14.12 14.12 7.76 16.24 9.88 9.88 16.24 7.76" />
  </svg>
);

export const ClipboardList: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <rect x="8" y="2" width="8" height="4" rx="1" />
    <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2" />
    <path d="M8 11h.01M8 16h.01" />
    <line x1="12" y1="11" x2="16" y2="11" />
    <line x1="12" y1="16" x2="16" y2="16" />
  </svg>
);

export const RefreshCw: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M3 12a9 9 0 0 1 15-6.7L21 8" />
    <path d="M21 3v5h-5" />
    <path d="M21 12a9 9 0 0 1-15 6.7L3 16" />
    <path d="M3 21v-5h5" />
  </svg>
);

export const Trophy: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6" />
    <path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18" />
    <path d="M6 3h12v5a6 6 0 0 1-12 0z" />
    <line x1="12" y1="14" x2="12" y2="18" />
    <path d="M9 21h6" />
    <path d="M10 18h4" />
  </svg>
);

export const Activity: IconComponent = (p) => (
  <svg {...svgProps(p)}>
    <path d="M22 12h-4l-3 9L9 3l-3 9H2" />
  </svg>
);
