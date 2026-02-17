PRD Generator

GPT-5 기반 PRD 초안 생성 API
제품 아이디어를 구조화된 PRD 문서로 자동 생성한다.

Overview

PRD Generator는 다음을 목표로 한다.

제품 아이디어를 구조화된 PRD 초안으로 변환

기능 요구사항과 비기능 요구사항을 분리

제약 조건을 준수한 실행 가능한 문서 생성

Jira 및 Confluence에 바로 붙여넣기 가능한 포맷 지원

Features

GPT-5 Responses API 기반 구현

항상 한국어 출력

JSON 구조 강제

제약 조건 엄격 준수

Jira / Confluence Markdown 포맷 지원

Run
1. 환경 변수 설정

PowerShell 세션 기준

$env:OPENAI_API_KEY="your_key"
$env:OPENAI_MODEL="gpt-5.2"

2. 실행
dotnet run

API
Endpoint
POST /generate-prd

Request Example
{
  "productIdea": "사용자가 검색 필터를 저장하고 팀원과 공유할 수 있는 기능을 추가한다.",
  "targetUser": "B2B 환경에서 반복 검색을 수행하는 운영 담당자",
  "problem": "동일한 검색 조건을 반복 설정해야 하며 팀원과 공유가 어렵다.",
  "constraints": [
    "기존 인증 시스템을 사용해야 한다.",
    "새로운 데이터베이스는 도입하지 않는다."
  ],
  "timelineWeeks": 4,
  "outputFormat": "both"
}

Response Structure
{
  "problemStatement": "string",
  "targetUser": "string",
  "useCases": [],
  "functionalRequirements": [],
  "nonFunctionalRequirements": [],
  "outOfScope": [],
  "successMetrics": [],
  "risks": [],
  "formatted": {
    "jira": "string",
    "confluence": "string"
  }
}

Architecture

.NET 10 Minimal API

OpenAI Responses API

Strict JSON parsing

Stateless design

Version

v0.1.0
Initial release with GPT-5.2 and Responses API support.
