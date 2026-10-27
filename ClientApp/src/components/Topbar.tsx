
type TopbarProps = {
  activePage: string
}

export default function Topbar({ activePage }: TopbarProps) {
  return (
    <header className="topbar">
      <div>
        <p className="breadcrumb">DVLD / Management</p>
        <h1>{activePage}</h1>
      </div>

      <div className="topbar-actions">
        <span className="system-status">
          <span className="status-dot" />
          System Online
        </span>

        <div className="topbar-avatar">A</div>
      </div>
    </header>
  )
}
