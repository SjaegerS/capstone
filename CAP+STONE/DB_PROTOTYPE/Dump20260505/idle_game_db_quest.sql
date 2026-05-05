-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: idle_game_db
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `quest`
--

DROP TABLE IF EXISTS `quest`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `quest` (
  `quest_id` bigint NOT NULL AUTO_INCREMENT,
  `quest_name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `quest_description` text COLLATE utf8mb4_unicode_ci,
  `quest_type` enum('BATTLE','USAGE','AI','DAILY','GOLD') COLLATE utf8mb4_unicode_ci NOT NULL,
  `target_value` int NOT NULL,
  `reward_gold` int DEFAULT '0',
  `reward_gem` int DEFAULT '0',
  `reward_exp` int DEFAULT '0',
  `condition_recovery` int DEFAULT '0',
  `is_active` tinyint(1) DEFAULT '1',
  PRIMARY KEY (`quest_id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `quest`
--

LOCK TABLES `quest` WRITE;
/*!40000 ALTER TABLE `quest` DISABLE KEYS */;
INSERT INTO `quest` VALUES (1,'보스 10마리 처치','자동 전투를 통해 보스 몬스터를 10마리 처치하세요.','BATTLE',10,300,0,50,1,1),(2,'12시~6시 휴대폰 사용 줄이기','자정부터 오전 6시까지 휴대폰 사용을 줄이세요.','USAGE',1,500,1,80,2,1),(3,'특정 앱 사용시간 줄이기','지정된 앱의 사용시간을 목표치 이하로 유지하세요.','USAGE',1,500,1,80,2,1),(4,'AI 피드백 확인하기','오늘의 생활패턴 분석 결과를 확인하세요.','AI',1,200,0,30,1,1),(5,'골드의 탑 보상 수령','골드 콘텐츠에서 보상을 1회 수령하세요.','GOLD',1,400,0,40,1,1),(6,'일일 접속 보상 받기','오늘 게임에 접속하세요.','DAILY',1,150,0,20,0,1);
/*!40000 ALTER TABLE `quest` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-05-05 18:08:33
