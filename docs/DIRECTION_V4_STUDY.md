# 🚀 EasySave — Backup Optimization and v4.0 Roadmap

> Answer to the ProSoft management's request for the v3.0 presentation: future evolutions (v4.0) and backup optimization, with a customer benefit / development time study.

---

## 1. Is it relevant to run backups in parallel?

| Storage | Benefit of parallelism | Why |
|---|:---:|---|
| **Hard disk drive (HDD)** | ⚠️ Low, can be counter-productive | The read heads keep moving between files (*head thrashing*); seek times make throughput drop. |
| **SSD / NVMe** | 🟢 High | Concurrent reads and writes with deep command queues; large backups finish much faster. |
| **Network (LAN / NAS)** | 🟡 Depends on bandwidth | Useful on a 1–10 Gbps network, as long as large transfers do not saturate the link: this is why v3.0 forbids two files above n KB at the same time. |

**Conclusion:** parallel backups pay off on modern storage (SSD, enterprise network) **provided that** transfers are regulated, as v3.0 does with the large file rule (`LargeFileTransferGuard`) and the priority file rule (`PriorityTransferGuard`). For HDD targets, a v4.0 option could limit the number of jobs running at the same time.

---

## 2. Proposed evolutions for v4.0

| # | Evolution | Customer benefit | Development time | Priority |
|:---:|---|:---:|:---:|:---:|
| 1 | Built-in scheduler | 🟢 High | 🟢 1–2 days | ⭐⭐⭐⭐⭐ |
| 2 | Compression and deduplication | 🟢 High | 🟡 3–5 days | ⭐⭐⭐⭐ |
| 3 | Cloud / SFTP targets | 🟢 Very high | 🟡🔴 1–2 weeks | ⭐⭐⭐⭐ |
| 4 | Web administration console | 🟡 Medium | 🔴 2–3 weeks | ⭐⭐⭐ |

### 1. Built-in scheduler
- **What:** schedule jobs (every night at 2 a.m., every Friday...) without the Windows Task Scheduler.
- **Benefit:** fully automated backups; nobody needs to start them by hand.
- **Cost:** short, with a library such as `Quartz.NET` or `Cronos`.

### 2. Compression and deduplication
- **What:** compress backups (`.zip`) and avoid storing the same file twice (SHA-256 fingerprints).
- **Benefit:** 50–80 % less disk space on full backups.
- **Cost:** medium (`System.IO.Compression`, fingerprint index).

### 3. Cloud and SFTP targets (AWS S3, Azure Blob Storage, SFTP)
- **What:** send backups to an off-site bucket or a secure SFTP server.
- **Benefit:** business continuity after a fire or a ransomware attack.
- **Cost:** medium to high (provider APIs, resuming after network failures).

### 4. Web administration console (Blazor / REST API)
- **What:** a web dashboard on the internal network to manage every EasySave installation, built on top of the central log server.
- **Benefit:** easier administration for IT teams.
- **Cost:** high (API security, new interface).

---

## 3. Key messages for the presentation

1. **Parallelism:** parallel backups greatly reduce backup time on SSD and network storage, and the large file and priority rules keep the network and the important files under control.
2. **v4.0:** the benefit / cost study puts the scheduler and compression first (quick wins), then cloud targets in a later phase.
