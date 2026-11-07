import { Users, ShieldCheck, UserCog, UserCheck } from "lucide-react";
import StatsGrid from "../common/StatsGrid";
import StatCard from "../common/StatCard";
import { formatNumber } from "../../utils/formatters";

export interface UsersSummaryProps {
  totalUsersCount: number;
  adminUsersCount: number;
  staffUsersCount: number;
  activeUsersCount: number;
  inactiveUsersCount: number;
  loading: boolean;
  loadError: string | null;
}

export default function UsersSummary({
  totalUsersCount,
  adminUsersCount,
  staffUsersCount,
  activeUsersCount,
  inactiveUsersCount,
  loading,
  loadError,
}: UsersSummaryProps) {
  return (
    <StatsGrid columns={4}>
      <StatCard
        label="Total Users"
        variant="blue"
        icon={<Users size={20} />}
        value={loading ? "..." : loadError ? "—" : formatNumber(totalUsersCount)}
        description="Registered system accounts"
      />

      <StatCard
        label="Administrators"
        variant="violet"
        icon={<ShieldCheck size={20} />}
        value={loading ? "..." : loadError ? "—" : formatNumber(adminUsersCount)}
        description="Full administrative access"
      />

      <StatCard
        label="Staff Accounts"
        variant="amber"
        icon={<UserCog size={20} />}
        value={loading ? "..." : loadError ? "—" : formatNumber(staffUsersCount)}
        description="Licensing & operational staff"
      />

      <StatCard
        label="Active Accounts"
        variant="emerald"
        icon={<UserCheck size={20} />}
        value={loading ? "..." : loadError ? "—" : formatNumber(activeUsersCount)}
        description={
          loading || loadError
            ? "Authorized sign-in sessions"
            : `${formatNumber(inactiveUsersCount)} inactive account${inactiveUsersCount === 1 ? "" : "s"}`
        }
      />
    </StatsGrid>
  );
}
