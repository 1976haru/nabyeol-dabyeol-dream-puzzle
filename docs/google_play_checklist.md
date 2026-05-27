# Google Play 등록 체크리스트 (사람이 직접)

Capacitor Android 프로젝트는 `web-game/android` 에 생성되어 있습니다.

## 순서
1. https://play.google.com/console 에서 개발자 등록 (최초 1회 25달러)
2. Android Studio 설치 후 `web-game/android` 폴더 열기
   - 코드 변경 후 반영: 터미널에서 `cd web-game && npm run build && npx cap sync android`
3. **Build → Generate Signed Bundle / APK → Android App Bundle(AAB)** 선택
4. 키스토어 생성 (`nabyeol.jks`) — 비밀번호/별칭을 안전하게 보관 (분실 시 업데이트 불가)
5. Play Console 에서 앱 등록
   - 앱 이름: 나별다별 꿈퍼즐
   - 패키지명: `com.nabyeol.dreampuzzle`
   - 카테고리: 게임 > 퍼즐
   - 연령등급: 전체 이용가 (개인정보 미수집)
6. 스토어 등록 자료
   - 앱 설명 (짧은 설명 / 자세한 설명)
   - 앱 아이콘 512×512 PNG (가이드: `web-game/public/icon.svg` 를 512px PNG 로 내보내기)
   - 스크린샷 2~8장 (실기기 또는 에뮬레이터 캡처)
   - 그래픽 이미지 1024×500
7. 개인정보처리방침 URL:
   https://1976haru.github.io/nabyeol-dabyeol-dream-puzzle/privacy.html
8. 출시 단계: **내부 테스트 → 비공개 테스트 → 프로덕션** 순서로 제출

## 아이콘 PNG 만들기 (참고)
- 온라인 변환 또는 Inkscape 로 `public/icon.svg` → 512×512 PNG
- 앱 런처 아이콘은 Android Studio 의 Image Asset Studio 로 생성 권장
